#define OSM
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

using WSleeman.Osm;

using CIFPReader;

using static SectorGenerator.Helpers;

namespace SectorGenerator;

public class Program
{
	public static async Task Main()
	{
		Config config = File.Exists("config.json") ? JsonSerializer.Deserialize<Config>(File.ReadAllText("config.json")) : Config.Default;

		Console.Write("Getting IVAO API token..."); await Console.Out.FlushAsync();
		(string apiToken, string apiRefreshToken) = await GetApiKeysAsync(config);
		Console.WriteLine($" Done! (Refresh: {apiRefreshToken})");

#if OSM
		Console.Write("Queueing OSM data download..."); await Console.Out.FlushAsync();
		Osm? osm = null;

		Task osmLoader = Task.Run(async () =>
		{
			for (int iterations = 0; iterations < 3; ++iterations)
			{
				try
				{
					osm = await Osm.Load();
					break;
				}
				catch (TimeoutException) { /* Sometimes things choke. */ }
				catch (TaskCanceledException) { /* Sometimes things choke. */ }

				await Task.Delay(TimeSpan.FromSeconds(15)); // Give it a breather.
			}

			if (osm is null)
				throw new Exception("Could not download OSM data.");
		});

		Console.WriteLine(" Done!");
#endif

		List<ManualAdjustment> manualAdjustments = [];
		if (config.ManualAdjustmentsFolder is string maf && Directory.Exists(maf))
		{
			Console.Write($"Located manual adjustments... "); await Console.Out.FlushAsync();
			string[] files = [.. Directory.EnumerateFiles(maf, "*.maf", SearchOption.AllDirectories)];
			Console.Write($"{files.Length} files detected... "); await Console.Out.FlushAsync();

			foreach (string filepath in files)
				manualAdjustments.AddRange(ManualAdjustment.Process(File.ReadAllText(filepath)));

			Console.WriteLine($"Done! Loaded {manualAdjustments.Count} adjustments.");
		}

		Console.Write("Reading AIRAC data..."); await Console.Out.FlushAsync();
		CIFP cifp = new(config.AiracFile, "C");

		Dictionary<string, HashSet<((double Latitude, double Longitude)[] Points, string Label)>> firBoundaries = cifp.FirBoundaries;
		Dictionary<string, string[]> firNeighbours = cifp.FirNeighbours;
		string[] targetFirs = [.. firBoundaries.Keys.Where(k => k[0] == 'C')];
		bool IsInFir(string fir, ICoordinate point) => firBoundaries[fir].Any(b => IsInPolygon(b.Points, point));
		Console.WriteLine(" Done!");

		foreach (var airport in cifp.Aerodromes.Values)
			if (cifp.Fixes.TryGetValue(airport.Identifier, out var apFixes))
				apFixes.Add(airport.Location);
			else
				cifp.Fixes[airport.Identifier] = [airport.Location];

		foreach (var runway in cifp.Runways.Values.SelectMany(r => r))
			if (cifp.Fixes.TryGetValue($"{runway.Airport}/RW{runway.Identifier}", out var rwyFixes))
				rwyFixes.Add(runway.Endpoint);
			else
				cifp.Fixes[$"{runway.Airport}/RW{runway.Identifier}"] = [runway.Endpoint];

		Dictionary<string, HashSet<AddProcedure>> addedProcedures = [];
		HashSet<NamedCoordinate> vfrFixes = [];
		HashSet<ICoordinate[]> vfrRoutes = [];
		Dictionary<string, (string Colour, HashSet<IDrawableGeo> Drawables)> videoMaps = [];
		if (manualAdjustments.Count > 0)
		{
			Console.Write($"Applying {manualAdjustments.Count} manual adjustments..."); await Console.Out.FlushAsync();

			// Fixes
			foreach (AddFix af in manualAdjustments.Where(a => a is AddFix).Cast<AddFix>())
				if (cifp.Fixes.TryGetValue(af.Name, out var prevKnownFixes))
				{
					ICoordinate newFix = af.Position.Resolve(cifp);
					Coordinate newFixPos = newFix.GetCoordinate();

					if (!prevKnownFixes.Any(pnf => pnf.Latitude == newFixPos.Latitude && pnf.Longitude == newFixPos.Longitude))
						prevKnownFixes.Add(newFix);
				}
				else
					cifp.Fixes.Add(af.Name, [af.Position.Resolve(cifp)]);

			foreach (RemoveFix rf in manualAdjustments.Where(a => a is RemoveFix).Cast<RemoveFix>())
				cifp.Fixes.Remove(rf.Fix.Coordinate is NamedCoordinate nc ? nc.Name : rf.Fix.FixName!.Name);

			// VFR fixes
			foreach (AddVfrFix vf in manualAdjustments.Where(a => a is AddVfrFix).Cast<AddVfrFix>())
				vfrFixes.Add(new(vf.Name, vf.Position.Resolve(cifp).GetCoordinate()));

			// VFR routes
			foreach (AddVfrRoute vr in manualAdjustments.Where(a => a is AddVfrRoute).Cast<AddVfrRoute>())
				vfrRoutes.Add([.. vr.Points.Select(p => {
					ICoordinate resolvedCoord = p.Resolve(cifp);

					if (resolvedCoord is NamedCoordinate nc)
					{
						if (cifp.Fixes.TryGetValue(nc.Name, out var prevKnownFixes))
						{
							if (!prevKnownFixes.Contains(nc))
								prevKnownFixes.Add(nc);
						}
						else
							cifp.Fixes.Add(nc.Name, [nc]);

						vfrFixes.Add(nc);
					}

					return resolvedCoord;
				})]);

			// Airways (in-place)
			HashSet<PossiblyResolvedWaypoint> failedResolutions = [];
			foreach (AddAirway aw in manualAdjustments.Where(a => a is AddAirway).Cast<AddAirway>())
				for (int idx = 0; idx < aw.Points.Length; ++idx)
				{
					try
					{
						aw.Points[idx] = new(aw.Points[idx].Resolve(cifp), null, null);
					}
					catch
					{
						failedResolutions.Add(aw.Points[idx]);
					}
				}

			if (failedResolutions.Count > 0)
			{
				Console.Error.WriteLine("Waypoint resolution failed! Please define the following waypoints manually:");
				foreach (PossiblyResolvedWaypoint wp in failedResolutions)
					Console.Error.WriteLine(wp.FixName?.Name ?? wp.ToString());

				Environment.Exit(-1);
			}

			// Videomaps
			foreach (AddGeo geo in manualAdjustments.Where(a => a is AddGeo).Cast<AddGeo>())
				if (videoMaps.TryGetValue(geo.Tag, out var preExistingGeos))
				{
					preExistingGeos.Drawables.UnionWith(geo.Geos.Select(g => { g.Resolve(cifp); return g; }));

					if (preExistingGeos.Colour == "#FF999999")
						videoMaps[geo.Tag] = (geo.Colour, preExistingGeos.Drawables);
				}
				else
					videoMaps.Add(geo.Tag, (geo.Colour, [.. geo.Geos.Select(g => { g.Resolve(cifp); return g; })]));

			// Procedures
			addedProcedures = manualAdjustments
				.Where(a => a is AddProcedure).Cast<AddProcedure>()
				.GroupBy(p => p.Airport)
				.ToDictionary(
					g => g.Key,
					g => g.Select(i => i with { Geos = [.. i.Geos.SelectMany<IDrawableGeo, IDrawableGeo>(g => g.Resolve(cifp) ? [g] : [])] }).ToHashSet()
				);

			foreach (RemoveProcedure proc in manualAdjustments.Where(a => a is RemoveProcedure).Cast<RemoveProcedure>())
				if (cifp.Procedures.TryGetValue(proc.Identifier, out var procs))
					procs.RemoveWhere(p => p.Airport == proc.Airport);

			Console.WriteLine(" Done!");
		}

		// Generate copy-pasteable Webeye shapes for each of the ARTCCs.
		(string Fir, string Shape)[] firWebeyeShapes = [..
			firBoundaries.Where(b => targetFirs.Contains(b.Key)).Select(b => (
				b.Key,
				string.Join("\r\n", b.Value.SelectMany(v => v.Points).Reverse().Select(p => $"{p.Latitude:00.0####}:{(p.Longitude > 0 ? p.Longitude - 360 : p.Longitude):000.0####}").ToArray())
			))
		];

		if (!Directory.Exists("webeye"))
			Directory.CreateDirectory("webeye");

		foreach (var (fir, shape) in firWebeyeShapes)
			File.WriteAllText(Path.ChangeExtension(Path.Join("webeye", fir), "txt"), shape);

		Console.Write("Allocating airports to centers..."); await Console.Out.FlushAsync();
		Dictionary<string, HashSet<Aerodrome>> centerAirports = [];

		foreach (var fir in targetFirs)
			centerAirports.Add(fir, [..
				cifp.Aerodromes.Values.Where(a => IsInFir(fir, a.Location))
					.Concat(config.SectorAdditionalAirports.TryGetValue(fir, out var addtl) ? addtl.SelectMany<string, Aerodrome>(a => cifp.Aerodromes.TryGetValue(a, out var r) ? [r] : []) : [])
			]);

		Console.WriteLine(" Done!");

		Console.Write("Allocating runways to centers..."); await Console.Out.FlushAsync();
		Dictionary<string, HashSet<(string Airport, HashSet<Runway> Runways)>> centerRunways = [];

		foreach (var fir in targetFirs)
			centerRunways.Add(fir, [.. cifp.Runways.Where(kvp => centerAirports[fir].Select(ad => ad.Identifier).Contains(kvp.Key)).Select(kvp => (kvp.Key, kvp.Value))]);

		Console.WriteLine(" Done!");

		Console.Write("Getting ATC positions..."); await Console.Out.FlushAsync();
		var atcPositions = await GetAtcPositionsAsync(apiToken, "C");
		var airportFirLookup = centerAirports.SelectMany(ca => ca.Value.Select(v => new KeyValuePair<string, string>(v.Identifier, ca.Key)).Append(new(ca.Key, ca.Key))).DistinctBy(kvp => kvp.Key).ToDictionary();
		Dictionary<string, JsonObject[]> positionFirs =
			atcPositions
			.GroupBy(p => airportFirLookup.TryGetValue(p["composePosition"]!.GetValue<string>().Split("_")[0], out var fir) ? fir : "ZZZZ")
			.ToDictionary(g => g.Key, g => g.ToArray());

		Console.WriteLine(" Done!");

		// Make sure folders are in place.
		if (!Directory.Exists(config.OutputFolder))
			Directory.CreateDirectory(config.OutputFolder);

		foreach (string existingIsc in Directory.EnumerateFiles(config.OutputFolder, "*.isc"))
			File.Delete(existingIsc);

		string includeFolder = Path.Combine(config.OutputFolder, "Include");
		if (Directory.Exists(includeFolder))
			Directory.Delete(includeFolder, true);

		Directory.CreateDirectory(includeFolder);
		includeFolder = Path.Combine(includeFolder, "CA");
		Directory.CreateDirectory(includeFolder);
		string polygonFolder = Path.Combine(includeFolder, "polygons");
		Directory.CreateDirectory(polygonFolder);

		Console.Write("Generating shared navigation data..."); await Console.Out.FlushAsync();
		WriteNavaids(includeFolder, cifp);
		Console.WriteLine(" Done!");

		Console.Write("Generating procedures..."); await Console.Out.FlushAsync();
		var apProcFixes = await WriteProceduresAsync(cifp, addedProcedures, includeFolder);
		Console.WriteLine($" Done!");

		Console.Write("Generating video maps..."); await Console.Out.FlushAsync();
		string videoMapsFolder = Path.Combine(includeFolder, "videomaps");
		if (!Directory.Exists(videoMapsFolder))
			Directory.CreateDirectory(videoMapsFolder);

		WriteVideoMaps(videoMaps, videoMapsFolder);
		Console.WriteLine($" Done!");

#if OSM
		Console.Write("Awaiting OSM download..."); await Console.Out.FlushAsync();
		await osmLoader;
		Console.WriteLine(" Done!");

		Console.Write("Partitioning airport data..."); await Console.Out.FlushAsync();
		Osm apBoundaries = osm!.GetFiltered(g =>
			g is Way or Relation &&
			g["aeroway"] == "aerodrome" &&
			g["icao"] is not null &&
			g["abandoned"] is null
		);

		Dictionary<string, Way> apBoundaryWays = apBoundaries.WaysAndBoundaries()
				.Select(w => (w["icao"], w))
				.OrderBy(kvp => kvp.w.Tags.ContainsKey("military") ? 1 : 0)
				.DistinctBy(kvp => kvp.Item1)
				.Where(kvp => kvp.Item1 is not null)
				.ToDictionary(kvp => kvp.Item1!, kvp => kvp.w);

		IDictionary<string, Osm> apOsms = osm.GetFiltered(item => item is not Node n || n["aeroway"] is "parking_position").Group(
			apBoundaryWays,
			30
		);

		Dictionary<string, Way[]> firOsmOnlyIcaos =
			apBoundaries.GetFiltered(apb => !cifp.Aerodromes.ContainsKey(apb["icao"]!)).Group(
				firBoundaries
				.Where(kvp => targetFirs.Contains(kvp.Key))
				.ToDictionary(
					b => b.Key,
					b => new Way(0, [.. b.Value.SelectMany(v => v.Points).Select(n => new Node(0, n.Latitude, n.Longitude, FrozenDictionary<string, string>.Empty))], FrozenDictionary<string, string>.Empty)
				)
			).ToDictionary(
				kvp => kvp.Key,
				kvp => kvp.Value.WaysAndBoundaries().ToArray());

		Console.WriteLine($" Done!");
		Console.Write("Generating labels, centerlines, and coastline..."); await Console.Out.FlushAsync();
		WriteGeos(includeFolder, apOsms);
		Console.WriteLine($" Done!");
		Console.Write("Generating polygons..."); await Console.Out.FlushAsync();

		var polygonBlocks = apOsms.AsParallel().AsUnordered().Select(input =>
		{
			var (icao, apOsm) = input;
			StringBuilder tfls = new();

			// Aprons
			foreach (Way apron in apOsm.GetFiltered(g => g["aeroway"] is "apron").WaysAndBoundaries())
			{
				tfls.AppendLine($"STATIC;APRON;1;APRON;");

				foreach (Node n in apron.Nodes.Append(apron.Nodes[0]))
					tfls.AppendLine($"{n.Latitude:00.0#####};{n.Longitude:000.0#####};");
			}

			// Buildings
			foreach (Way building in apOsm.GetFiltered(g => g["aeroway"] is "terminal" or "hangar").WaysAndBoundaries())
			{
				tfls.AppendLine($"STATIC;BUILDING;1;BUILDING;");

				foreach (Node n in building.Nodes.Append(building.Nodes[0]))
					tfls.AppendLine($"{n.Latitude:00.0#####};{n.Longitude:000.0#####};");
			}

			// Taxiways
			Taxiways taxiways = new(
				icao,
				apOsm.GetFiltered(g => g is Way w && w["aeroway"] is "taxiway")
			);
			foreach (Way txw in taxiways.BoundingBoxes)
			{
				tfls.AppendLine($"STATIC;TAXIWAY;1;TAXIWAY;");

				foreach (Node n in txw.Nodes)
					tfls.AppendLine($"{n.Latitude:00.0#####};{n.Longitude:000.0#####};");
			}

			// Helipads
			foreach (Way helipad in apOsm.GetFiltered(g => g["aeroway"] is "helipad").WaysAndBoundaries())
			{
				tfls.AppendLine($"STATIC;RUNWAY;1;RUNWAY;");

				foreach (Node n in helipad.Nodes)
					tfls.AppendLine($"{n.Latitude:00.0#####};{n.Longitude:000.0#####};");
			}

			double rwWidth = cifp.Runways.TryGetValue(icao, out var rws) ? rws.Average(rw => rw.Width * 0.00000137) : 0.0002;
			// Runways
			foreach (Way rw in apOsm.GetFiltered(g => g["aeroway"] is "runway").WaysAndBoundaries().Select(rw => rw.Inflate(rwWidth)))
			{
				tfls.AppendLine($"STATIC;RUNWAY;1;RUNWAY;");

				foreach (Node n in rw.Nodes)
					tfls.AppendLine($"{n.Latitude:00.0#####};{n.Longitude:000.0#####};");
			}

			return (icao, tfls.ToString());
		});

		foreach (var (icao, tfl) in polygonBlocks.Where(i => i.Item2.Length > 0))
			File.WriteAllText(Path.Combine(polygonFolder, icao + ".tfl"), tfl);

		Console.WriteLine($" Done!");
#endif

		File.WriteAllText
			(Path.Combine(polygonFolder, "online.ply"),
			string.Join(
				"\r\n\r\n",
				atcPositions
					.Where(p => p["regionMap"] is JsonArray region && region.Count > 0)
					.Select(p => WebeyeAirspaceDrawing.ToPolyfillPath(p["composePosition"]!.GetValue<string>(), p["position"]?.GetValue<string>() ?? "TWR", p["regionMap"]!.AsArray()))
					.Concat(firBoundaries.Where(fir => fir.Value.Count > 0).Select(fir => WebeyeAirspaceDrawing.ToPolyfillPath($"{fir.Key}_CTR", "CTR", fir.Value.SelectMany(kvp => kvp.Points).ToArray())))
			)
#if OSM
+ "\r\n\r\n" + string.Join("\r\n\r\n",
		centerAirports
			.Values
			.SelectMany(adg => adg.Select(ad => apBoundaryWays.TryGetValue(ad.Identifier, out var retval) ? (ad.Identifier, retval) : ((string, Way)?)null))
			.Where(ap => ap is not null)
			.Cast<(string Icao, Way Boundary)>()
			.Select(ap => WebeyeAirspaceDrawing.ToPolyfillPath(ap.Icao + "_TWR", "TWR", ap.Boundary))
	)
#endif
);

		// Write ISCs
		foreach (string fir in targetFirs)
		{
			string mvaFolder = Path.Combine(includeFolder, "mvas");
			if (!Directory.Exists(mvaFolder))
				Directory.CreateDirectory(mvaFolder);

			Airport[] ifrAirports = [.. centerAirports[fir].Where(ad => ad is Airport ap && ap.IFR).Cast<Airport>()];

			if (ifrAirports.Length == 0)
			{
				Console.Write($"({fir} skipped (no airports)) ");
				return;
			}

			(double Latitude, double Longitude) centerpoint = (
				ifrAirports.Average(ap => (double)ap.Location.Latitude),
				ifrAirports.Average(ap => (double)(ap.Location.Longitude > 0 ? ap.Location.Longitude - 360 : ap.Location.Longitude))
			);

			// Info
			double cosLat = Math.Cos(centerpoint.Latitude * Math.PI / 180);

			string infoBlock = $@"[INFO]
{Dms(centerpoint.Latitude, false)}
{Dms(centerpoint.Longitude, true)}
60
{60 * Math.Abs(cosLat):00}
{-ifrAirports.Average(ap => ap.MagneticVariation):00.0000}
CA/{fir};CA/labels;CA/geos;CA/polygons;CA/procedures;CA/navaids;CA/mvas;CA/videomaps
";
			string firFolder = Path.Combine(includeFolder, fir);
			if (!Directory.Exists(firFolder))
				Directory.CreateDirectory(firFolder);

			string[] applicableVideoMaps = [.. videoMaps.Where(kvp => kvp.Value.Drawables.Any(g => g.ReferencePoints.Any(p => IsInFir(fir, p)))).Select(kvp => kvp.Key)];

			// Colours
			string defineBlock = $@"[DEFINE]
TAXIWAY;#FF353B42;
APRON;#FF26292E;
OUTLINE;#FF000000;
BUILDING;#FF5C3630;
RUNWAY;#FF1A1A1A;
STOPBAR;#FFB30000;
{string.Join("\r\n", applicableVideoMaps.Select(g => $"{g};{videoMaps[g].Colour};"))}
";

			// ATC Positions
			string atcBlock = "[ATC]\r\nF;atc.atc\r\n";
			if (positionFirs.TryGetValue(fir, out var firPositions))
			{
				string allPositions = string.Join(' ', firPositions.Select(p => p["composePosition"]!.GetValue<string>()));
				File.WriteAllLines(Path.Combine(firFolder, "atc.atc"), [
					..firPositions.Select(p => $"{p["composePosition"]!.GetValue<string>()};{p["frequency"]!.GetValue<decimal>():000.000};{allPositions};"),
					..atcPositions.Where(p => p["centerId"]?.GetValue<string>() == fir).Select(p => $"{p["composePosition"]!.GetValue<string>()};{p["frequency"]!.GetValue<decimal>():000.000};{allPositions};")
				]);
			}

			// Airports (main)
			string airportBlock = "[AIRPORT]\r\nF;airports.ap\r\n";
			File.WriteAllLines(Path.Combine(firFolder, "airports.ap"), [..
			centerAirports[fir].Select(ad => $"{ad.Identifier};{ad.Elevation.ToMSL().Feet};18000;{ad.Location.Latitude:00.0####};{ad.Location.Longitude:000.0####};{ad.Name.TrimEnd()};")
#if OSM
				.Concat(
					firOsmOnlyIcaos.TryGetValue(fir, out var aooi)
					? aooi.Select(w => $"{w["icao"]!};0;18000;{w.Nodes.Average(n => n.Latitude):00.0####};{w.Nodes.Average(n => n.Longitude):000.0####};{w["name"] ?? "Unknown Airport"};")
					: []
				)
#endif
			]);

			// Runways
			string runwayBlock = "[RUNWAY]\r\nF;runways.rw\r\n";
			File.WriteAllText(Path.Combine(firFolder, "runways.rw"), string.Join(
			"\r\n",
			centerRunways[fir].SelectMany(crg =>
				crg.Runways
					.Where(rw => rw.Identifier.CompareTo(rw.OppositeIdentifier) <= 0 && crg.Runways.Any(rw2 => rw.OppositeIdentifier == rw2.Identifier))
					.Select(rw => (Primary: rw, Opposite: crg.Runways.First(rw2 => rw2.Identifier == rw.OppositeIdentifier)))
					.Select(rws => $"{crg.Airport};{rws.Primary.Identifier};{rws.Opposite.Identifier};{rws.Primary.TDZE.ToMSL().Feet};{rws.Opposite.TDZE.ToMSL().Feet};" +
								   $"{(int)rws.Primary.Course.Degrees};{(int)rws.Opposite.Course.Degrees};" +
								   $"{rws.Primary.Endpoint.Latitude:00.0####};{rws.Primary.Endpoint.Longitude:000.0####};{rws.Opposite.Endpoint.Latitude:00.0####};{rws.Opposite.Endpoint.Longitude:000.0####};")
			)
		));

			// Airways
			Airway[] inScopeLowAirways = [..
				cifp.Airways.Where(kvp => kvp.Key[0] is 'V' or 'T')
				.Where(kvp => !manualAdjustments.Any(a => a is RemoveAirway raw && raw.Identifier.Equals(kvp.Key, StringComparison.InvariantCultureIgnoreCase)))
				.SelectMany(kvp => kvp.Value.Where(v => v.Count() >= 2 && v.Any(p => IsInFir(fir, p.Point))))
			];

			Airway[] inScopeHighAirways = [..
				cifp.Airways.Where(kvp => kvp.Key[0] is 'Q' or 'J')
					.Where(kvp => !manualAdjustments.Any(a => a is RemoveAirway raw && raw.Identifier.Equals(kvp.Key, StringComparison.InvariantCultureIgnoreCase)))
				.SelectMany(kvp => kvp.Value.Where(v => v.Count() >= 2 && v.Any(p => IsInFir(fir, p.Point))))
			];

			string airwaysBlock = $@"[LOW AIRWAY]
F;airways.low

[HIGH AIRWAY]
F;airways.high
";

			File.WriteAllLines(Path.Combine(firFolder, "airways.low"), [
				// AIRAC
				..inScopeLowAirways.SelectMany(v => (string[])[
					$"L;{v.Identifier};{v.Skip(v.Count() / 2).First().Point.Latitude:00.0####};{v.Skip(v.Count() / 2).First().Point.Longitude:000.0####};",
					..v.Select(p => $"T;{v.Identifier};{p.Name ?? p.Point.Latitude.ToString("00.0####")};{p.Name ?? p.Point.Longitude.ToString("000.0####")};")
				]),
				
				// Manual additions
				..manualAdjustments.Where(a => a is AddAirway aw && aw.Type == AddAirway.AirwayType.Low && aw.Points.Any(p => IsInFir(fir, p.Coordinate ?? new Coordinate()))).Cast<AddAirway>().SelectMany(aw => (string[])[
					$"L;{aw.Identifier};{aw.Points.Skip(aw.Points.Length / 2).First().Coordinate!.Latitude:00.0####};{aw.Points.Skip(aw.Points.Length / 2).First().Coordinate!.Longitude:000.0####};",
					..aw.Points.Select(p => {
						if (p.Coordinate is NamedCoordinate nc)
							return $"T;{aw.Identifier};{nc.Name};{nc.Name};";
						else
							return $"T;{aw.Identifier};{p.Coordinate!.Latitude:00.0####};{p.Coordinate.Longitude:000.0####};";
					})
				])
			]);

			File.WriteAllLines(Path.Combine(firFolder, "airways.high"), [
				// AIRAC
				..inScopeHighAirways.SelectMany(v => (string[])[
					$"L;{v.Identifier};{v.Skip(v.Count() / 2).First().Point.Latitude:00.0####};{v.Skip(v.Count() / 2).First().Point.Longitude:000.0####};",
					..v.Select(p => $"T;{v.Identifier};{p.Name ?? p.Point.Latitude.ToString("00.0####")};{p.Name ?? p.Point.Longitude.ToString("000.0####")};")
				]),

				// Manual additions
				..manualAdjustments.Where(a => a is AddAirway aw && aw.Type == AddAirway.AirwayType.High && aw.Points.Any(p => IsInFir(fir, p.Coordinate ?? new Coordinate()))).Cast<AddAirway>().SelectMany(aw => (string[])[
					$"L;{aw.Identifier};{aw.Points.Skip(aw.Points.Length / 2).First().Coordinate!.Latitude:00.0####};{aw.Points.Skip(aw.Points.Length / 2).First().Coordinate!.Longitude:000.0####}",
					..aw.Points.Select(p => {
						if (p.Coordinate is NamedCoordinate nc)
							return $"T;{aw.Identifier};{nc.Name};{nc.Name};";
						else
							return $"T;{aw.Identifier};{p.Coordinate!.Latitude:00.0####};{p.Coordinate.Longitude:000.0####};";
					})
				])
			]);

			// Fixes
			string fixesBlock = "[FIXES]\r\nF;fixes.fix\r\n";
			(string Key, Coordinate Point)[] fixes = [..cifp.Fixes.SelectMany(g => g.Value.Select(v => (g.Key, Point: v.GetCoordinate())))
		.Where(f => IsInFir(fir, f.Point))
		.Concat(cifp.Navaids.SelectMany(g => g.Value.Select(v => (g.Key, Point: v.Position))))];

			File.WriteAllLines(Path.Combine(firFolder, "fixes.fix"), [..
		fixes
			.Concat(inScopeLowAirways.SelectMany(aw => aw.Where(p => p.Name is string n && !fixes.Any(f => f.Key == n))).Select(p => (Key: p.Name!, Point: p.Point.GetCoordinate())))
			.Concat(inScopeHighAirways.SelectMany(aw => aw.Where(p => p.Name is string n && !fixes.Any(f => f.Key == n))).Select(p => (Key: p.Name!, Point: p.Point.GetCoordinate())))
			.Concat(manualAdjustments.Where(a => a is AddAirway).Cast<AddAirway>().SelectMany(aw => aw.Points.Where(wp => wp.Coordinate is NamedCoordinate p && p.Name is string n && !fixes.Any(f => f.Key == n))).Select(p => { var nc = (NamedCoordinate)p.Coordinate!; return (Key: nc.Name!, Point: nc.Position); }))
			.Concat(centerAirports[fir].SelectMany(icao => apProcFixes.TryGetValue(icao.Identifier, out var fixes) ? fixes : []).Select(p => (Key: p.Name!, Point: p.GetCoordinate())))
			.Select(f => $"{f.Key};{f.Point.Latitude:00.0####};{f.Point.Longitude:000.0####};")
			]);

			// Navaids
			string navaidBlock = "[NDB]\r\nF;ndb.ndb\r\n\r\n[VOR]\r\nF;vor.vor\r\n";

			// ARTCC boundaries
			string artccBlock = $@"[ARTCC]
F;artcc.artcc

[ARTCC LOW]
F;low.artcc

[ARTCC HIGH]
F;high.artcc
";

			IEnumerable<string> generateBoundary(string fir)
			{
				int iter = 0;
				foreach (var (points, label) in firBoundaries[fir])
				{
					++iter;
					if (points.Length < 2)
						continue;

					yield return $"L;{label};{points.Average(bp => bp.Latitude):00.0####};{points.Average(bp => bp.Longitude):000.0####};7;";

					foreach (var bp in points.Append(points[0]))
						yield return $"T;{fir}_{iter};{bp.Latitude:00.0####};{bp.Longitude:000.0####};";
				}
			}

			if (firBoundaries.TryGetValue(fir, out var boundary) && boundary.Count > 0)
				File.WriteAllText(Path.Combine(firFolder, "artcc.artcc"), $@"{string.Join("\r\n", generateBoundary(fir))}
		{string.Join("\r\n", firNeighbours.TryGetValue(fir, out var neighbours) ? neighbours.Where(n => firBoundaries.TryGetValue(n, out var check) && check.Count > 0).Select(n => string.Join("\r\n", generateBoundary(n))) : [])}
		");

			CifpAirspaceDrawing ad = new(cifp.Airspaces.Where(ap => ap.Regions.Any(r => r.Boundaries.Any(b => IsInFir(fir, b.Vertex)))));
			File.WriteAllText(Path.Combine(firFolder, "low.artcc"), ad.ClassCPaths + "\r\n\r\n" + ad.ClassDPaths);
			File.WriteAllText(Path.Combine(firFolder, "high.artcc"),
				string.Join("\r\n",
					positionFirs[fir].Where(p => p["position"]?.GetValue<string>() is "APP" && p["regionMap"] is JsonArray region && region.Count > 0 && p["airportId"] is not null)
					.Select(p => WebeyeAirspaceDrawing.ToArtccPath(p["airportId"]!.GetValue<string>(), p["regionMap"]!.AsArray()))
				) + "\r\n\r\n" + ad.ClassBPaths + "\r\n\r\n" + ad.ClassBLabels
			);

			// VFR Fixes
			string vfrBlock = "[VFRENR]\r\nF;vfr.vfi\r\n\r\n";

			File.WriteAllLines(Path.Combine(firFolder, "vfr.vfi"), [..
		fixes
			.Where(f => f.Key.StartsWith("VC"))
					.Concat(vfrFixes.Select(f => (Key: f.Name, Point: f.GetCoordinate())))
					.Where(f => IsInFir(fir, f.Point))
			.Select(f => $"{f.Key};;{f.Point.Latitude:00.0####};{f.Point.Longitude:000.0####};")
			]);

			// VFR Routes
			vfrBlock += "[VFRROUTE]\r\nF;vfr.vrt\r\n\r\n";

			ICoordinate[][] applicableRoutes = [..
				vfrRoutes.Where(r => r.Any(c => IsInFir(fir, c)))
			];

			File.WriteAllLines(
				Path.Combine(firFolder, "vfr.vrt"),
				Enumerable.Range(0, applicableRoutes.Length).Select(routeIdx =>
					string.Join("\r\n", applicableRoutes[routeIdx].Select(r =>
					{
						if (r is NamedCoordinate nc)
							return $"{routeIdx + 1};{nc.Name};{nc.Name};";

						Coordinate c = r.GetCoordinate();
						return $"{routeIdx + 1};{c.Latitude:00.0####};{c.Longitude:000.0####};";
					})))
			);

			// Geo file references
			string geoBlock = @$"[GEO]
F;coast.geo
{string.Join("\r\n", applicableVideoMaps.Select(vm => $"F;{Path.ChangeExtension(vm, "geo")}"))}
{string.Join("\r\n", centerAirports[fir].Select(ap => $"F;{ap.Identifier}.geo"))}
";
#if OSM
			geoBlock += (firOsmOnlyIcaos.TryGetValue(fir, out var aoois) ? string.Join("\r\n", aoois.Where(ap => (ap["icao"] ?? ap["faa"]) is not null).Select(ap => $"F;{ap["icao"] ?? ap["faa"]}.geo")) : "") + "\r\n";
#endif

			// Polyfills for dynamic sectors
			string polyfillBlock = $@"[FILLCOLOR]
F;online.ply
";

			File.WriteAllText(Path.Combine(config.OutputFolder, $"{fir}.isc"), $@"{infoBlock}
{defineBlock}
{atcBlock}
{airportBlock}
{runwayBlock}
{fixesBlock}
{navaidBlock}
{airwaysBlock}
{vfrBlock}
{artccBlock}
{geoBlock}
{polyfillBlock}");

			Console.Write($"{fir} "); await Console.Out.FlushAsync();
		}

		Console.WriteLine(" All Done!");
	}

	static void WriteNavaids(string includeFolder, CIFP cifp)
	{
		string navaidFolder = Path.Combine(includeFolder, "navaids");
		Directory.CreateDirectory(navaidFolder);

		File.WriteAllLines(Path.Combine(navaidFolder, "ndb.ndb"), [..cifp.Navaids.SelectMany(kvp => kvp.Value).Where(nv => nv is NDB).Cast<NDB>()
			.Select(ndb => $"{ndb.Identifier} ({ndb.Name});{ndb.Channel};{ndb.Position.Latitude:00.0####};{ndb.Position.Longitude:000.0####};0;")
			.Concat(cifp.Navaids.SelectMany(kvp => kvp.Value).Where(nv => nv is DME).Cast<DME>()
			.Select(dme => $"{dme.Identifier} ({dme.Name});{dme.Channel};{dme.Position.Latitude:00.0####};{dme.Position.Longitude:000.0####};0;"))]);
		File.WriteAllLines(Path.Combine(navaidFolder, "vor.vor"), [..cifp.Navaids.SelectMany(kvp => kvp.Value).Where(nv => nv is VOR).Cast<VOR>()
			.Select(vor => $"{vor.Identifier} ({vor.Name});{vor.Frequency:000.000};{vor.Position.Latitude:00.0####};{vor.Position.Longitude:000.0####};0;")]);
	}

	static async Task<(string apiToken, string refreshToken)> GetApiKeysAsync(Config config)
	{
		using Oauth oauth = new();
		JsonNode jsonNode;
		if ((config.IvaoApiRefresh ?? Environment.GetEnvironmentVariable("IVAO_REFRESH")) is string apiRefresh)
			jsonNode = await oauth.GetOpenIdFromRefreshTokenAsync(apiRefresh);
		else
			jsonNode = await oauth.GetOpenIdFromBrowserAsync();

		return (
			jsonNode["access_token"]!.GetValue<string>(),
			jsonNode["refresh_token"]!.GetValue<string>()
		);
	}

	static void WriteGeos(string includeFolder, IDictionary<string, Osm> apOsms)
	{
		string geoFolder = Path.Combine(includeFolder, "geos");
		Directory.CreateDirectory(geoFolder);
		string labelFolder = Path.Combine(includeFolder, "labels");
		Directory.CreateDirectory(labelFolder);

		const double CHAR_WIDTH = 0.0001;
		foreach (var (icao, apOsm) in apOsms)
		{
			List<string> gtsLabels = [];

			// Aprons & Buildings
			foreach (Way location in apOsm.GetFiltered(g => g["aeroway"] is "apron" or "terminal" or "hangar" or "helipad" && ((g["name"] ?? g["ref"]) is not null)).WaysAndBoundaries())
			{
				string label = (location["name"] ?? location["ref"])!;
				gtsLabels.Add($"{label};{icao};{location.Nodes.Average(n => n.Latitude) - CHAR_WIDTH:00.0####};{location.Nodes.Average(n => n.Longitude) - label.Length * CHAR_WIDTH / 2:000.0####};");
			}

			// Gates
			Gates gates = new(
				icao,
				apOsm.GetFiltered(g => g is Way w && w["aeroway"] is "parking_position")
			);
			gtsLabels.AddRange(gates.Labels.Split("\r\n", StringSplitOptions.RemoveEmptyEntries));
			File.WriteAllLines(Path.Combine(labelFolder, icao + ".gts"), [.. gtsLabels]);

			Osm taxiwayOsm = apOsm.GetFiltered(g => g is Way w && w["aeroway"] is "taxiway" or "taxilane");
			// Taxiways
			Taxiways taxiways = new(icao, taxiwayOsm);
			string[] txilabels = taxiways.Labels.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
			File.WriteAllLines(Path.Combine(labelFolder, icao + ".txi"), txilabels);

			StringBuilder stoplineGeos = new();
			// Stopbars
			foreach (Way stopline in apOsm.GetFiltered(g => g is Way w && w.Nodes.Length > 1 && w["aeroway"] is "holding_position").Ways.Values)
				foreach ((Node from, Node to) in stopline.Nodes[..^1].Zip(stopline.Nodes[1..]))
					stoplineGeos.AppendLine($"{from.Latitude:00.0####};{from.Longitude:000.0####};{to.Latitude:00.0####};{to.Longitude:000.0####};STOPLINE;");

			// Geos
			File.WriteAllText(Path.Combine(geoFolder, icao + ".geo"), gates.Routes + "\r\n\r\n" + taxiways.Centerlines + "\r\n\r\n" + stoplineGeos.ToString());
		}

		Way[] highCoastlineGeos = [.. Coastline.LoadTopologies("coastline")['h'].Where(w => w.Nodes.Length > 4 && w.Nodes.Any(n => n.Latitude > 40))];
		Way[] coarseCoastlineGeos = [.. Coastline.LoadTopologies("coastline")['c'].Where(w => w.Nodes.Length > 4 && w.Nodes.Any(n => n.Latitude > 40))];
		string[] coastLines = [..highCoastlineGeos.Where(w => w.Nodes.Length >= 2 && w.Nodes.Any(n => (n.Latitude >= 49 && n.Latitude <= 50 && n.Longitude <= -95 && n.Longitude >= -141) || (n.Latitude > 41 && n.Latitude <= 50 && n.Longitude > -95 && n.Longitude < -51))).Concat(coarseCoastlineGeos.Where(w => w.Nodes.Length >= 2 && w.Nodes.Any(n => n.Latitude > 50 && n.Longitude < -51 && n.Longitude >= -141)).Where(w => !w.Nodes.Any(n => n.Latitude < 50))).SelectMany(w =>
				w.BreakAntimeridian().Nodes.Zip(w.Nodes.Skip(1).Append(w.Nodes[0])).Select(np =>
					Math.Abs(np.First.Longitude - np.Second.Longitude) > 180
					? "// BREAK AT ANTIMERIDIAN."
					: $"{np.First.Latitude:00.0####};{np.First.Longitude:000.0####};{np.Second.Latitude:00.0####};{np.Second.Longitude:000.0####};COAST;"
				)
			)];

		File.WriteAllLines(Path.Combine(geoFolder, "coast.geo"), coastLines);
	}

	static void WriteVideoMaps(Dictionary<string, (string Colour, HashSet<IDrawableGeo> Drawables)> layers, string geoFolder) => Parallel.ForEach(layers, kvp =>
	{
		string layerName = kvp.Key;
		StringBuilder fileContents = new();
		fileContents.AppendLine($"// {layerName} - {kvp.Value.Drawables.Count} geos");

		foreach (IDrawableGeo geo in kvp.Value.Drawables)
		{
			ICoordinate? last = null;
			fileContents.AppendLine($"// {geo.GetType().Name}");

			foreach (ICoordinate? next in geo.Draw())
			{
				if (next is null)
					fileContents.AppendLine("// BREAK");
				else if (last is ICoordinate l && next is ICoordinate n)
					fileContents.AppendLine($"{l.Latitude:00.0####};{l.Longitude:000.0####};{n.Latitude:00.0####};{n.Longitude:000.0####};{layerName};");

				last = next;
			}
		}

		File.WriteAllText(Path.Combine(geoFolder, Path.ChangeExtension(layerName, ".geo")), fileContents.ToString());
	});

	static async Task<FrozenDictionary<string, HashSet<NamedCoordinate>>> WriteProceduresAsync(CIFP cifp, Dictionary<string, HashSet<AddProcedure>> addedProcedures, string includeFolder)
	{
		string procedureFolder = Path.Combine(includeFolder, "procedures");
		Directory.CreateDirectory(procedureFolder);

		ConcurrentDictionary<string, HashSet<NamedCoordinate>> apProcFixes = [];
		Procedures procs = new(cifp);

		Parallel.ForEach(cifp.Aerodromes.Values, airport =>
		{
			var (sidLines, sidFixes) = procs.AirportSidLines(airport.Identifier);
			var (starLines, starFixes) = procs.AirportStarLines(airport.Identifier);
			var (iapLines, iapFixes) = procs.AirportApproachLines(airport.Identifier);
			
			if (addedProcedures.TryGetValue(airport.Identifier, out var addedProcs))
			{
				foreach (var proc in addedProcs)
				{
					HashSet<NamedCoordinate> fixes = [];

					string[] allLines = [
						$"{airport.Identifier};{string.Join(':', cifp.Runways[airport.Identifier].Select(r => r.Identifier))};{proc.Identifier};{airport.Identifier};{airport.Identifier};{(proc.Type == AddProcedure.ProcedureType.IAP ? "3;" : "")}",
						..proc.Geos.Select(g =>
					{
						bool nextBreak = true;
						List<string> lines = [];

						foreach (ICoordinate? c in g.Draw())
						{
							if (c is null)
							{
								nextBreak = true;
								continue;
							}

							if (c is NamedCoordinate nc)
							{
								lines.Add($"{nc.Name};{nc.Name};{(nextBreak ? "<br>;" : "")}");
								fixes.Add(nc);
							}
							else
								lines.Add($"{c.Latitude:00.0####};{c.Longitude:000.0####};{(nextBreak ? "<br>;" : "")}");

							nextBreak = false;
						}

						return string.Join("\r\n", lines);
					})];

					switch (proc.Type)
					{
						case AddProcedure.ProcedureType.SID:
							sidLines = [.. sidLines, .. allLines];
							sidFixes = [.. sidFixes, .. fixes];
							break;

						case AddProcedure.ProcedureType.STAR:
							starLines = [.. starLines, .. allLines];
							starFixes = [.. starFixes, .. fixes];
							break;

						case AddProcedure.ProcedureType.IAP:
							iapLines = [.. iapLines, .. allLines];
							iapFixes = [.. iapFixes, .. fixes];
							break;
					}
				}
			}

			if (sidLines.Length > 0)
				File.WriteAllLines(Path.Combine(procedureFolder, airport.Identifier + ".sid"), sidLines);

			if (starLines.Length > 0)
			{
				File.WriteAllLines(Path.Combine(procedureFolder, airport.Identifier + ".str"), starLines);

				if (iapLines.Length > 0)
					File.AppendAllLines(Path.Combine(procedureFolder, airport.Identifier + ".str"), iapLines);
			}
			else if (iapLines.Length > 0)
				File.WriteAllLines(Path.Combine(procedureFolder, airport.Identifier + ".str"), iapLines);

			apProcFixes[airport.Identifier] = [.. sidFixes, .. starFixes, .. iapFixes];
		});

		return apProcFixes.ToFrozenDictionary();
	}
}