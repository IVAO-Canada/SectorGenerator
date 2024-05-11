﻿using CIFPReader;

using SectorGenerator;

using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

using WSleeman.Osm;

using static SectorGenerator.Helpers;

Config config = File.Exists("config.json") ? JsonSerializer.Deserialize<Config>(File.ReadAllText("config.json")) : Config.Default;

Console.Write("Getting IVAO API token...");
string apiToken, apiRefreshToken;
using (Oauth oauth = new())
{
	JsonNode jsonNode;
	if ((config.IvaoApiRefresh ?? Environment.GetEnvironmentVariable("IVAO_REFRESH")) is string apiRefresh)
		jsonNode = await oauth.GetOpenIdFromRefreshTokenAsync(apiRefresh);
	else
		jsonNode = await oauth.GetOpenIdFromBrowserAsync();

	apiToken = jsonNode["access_token"]!.GetValue<string>();
	apiRefreshToken = jsonNode["refresh_token"]!.GetValue<string>();
}
Console.WriteLine($" Done! (Refresh: {apiRefreshToken})");

// Long loading threads in parallel!
Console.Write("Downloading ARTCC boundaries, CIFPs, and OSM data...");
CIFP? cifp = null;
Osm? osm = null;
Dictionary<string, (double Latitude, double Longitude)[]> artccBoundaries = [];
Dictionary<string, string[]> artccNeighbours = [];
string[] faaArtccs = [];
await Task.WhenAll([
	Task.Run(async () => (artccBoundaries, artccNeighbours, faaArtccs) = await ArtccBoundaries.GetBoundariesAsync(config.BoundaryFilePath)),
	Task.Run(() => cifp = CIFP.Load()),
	Task.Run(async () => osm = await Osm.Load())
]);

// Keep the compiler happy with fallback checks.
cifp ??= CIFP.Load();
osm ??= await Osm.Load();
Console.WriteLine(" Done!");

Console.Write("Allocating airports to centers...");
Dictionary<string, HashSet<Aerodrome>> centerAirports = [];

// Generate copy-pasteable Webeye shapes for each of the ARTCCs.
(string Artcc, string Shape)[] artccWebeyeShapes = [..
	artccBoundaries.Select(b => (
		b.Key,
		string.Join("\r\n", b.Value.Reverse().Select(p => $"{(int)p.Latitude:00}{(int)(p.Latitude * 60 % 60):00}{(int)(p.Latitude * 360 % 60):00}N{(int)-p.Longitude:000}{(int)(-p.Longitude * 60 % 60):00}{(int)(-p.Longitude * 360 % 60):00}W").ToArray())
	))
];

if (!Directory.Exists("webeye"))
	Directory.CreateDirectory("webeye");

foreach (var (artcc, shape) in artccWebeyeShapes)
	File.WriteAllText(Path.ChangeExtension(Path.Join("webeye", artcc), "txt"), shape);

foreach (var (artcc, points) in artccBoundaries)
	centerAirports.Add(artcc, [..
		cifp.Aerodromes.Values.Where(a => IsInPolygon(points, ((double)a.Location.Latitude, (double)a.Location.Longitude)))
							  .Concat(config.SectorAdditionalAirports.TryGetValue(artcc, out var addtl) ? addtl.Select(a => cifp.Aerodromes[a]) : [])
	]);

Console.WriteLine(" Done!");

Console.Write("Allocating runways to centers...");
Dictionary<string, HashSet<(string Airport, HashSet<Runway> Runways)>> centerRunways = [];

foreach (var (artcc, points) in artccBoundaries)
	centerRunways.Add(artcc, [.. cifp.Runways.Where(kvp => centerAirports[artcc].Select(ad => ad.Identifier).Contains(kvp.Key)).Select(kvp => (kvp.Key, kvp.Value))]);

Console.WriteLine(" Done!");

Console.Write("Getting ATC positions...");
var atcPositions = await GetAtcPositionsAsync(apiToken, "K", faaArtccs.Select(a => "K" + a));
(JsonObject Position, string Artcc)[] positionArtccs = [..atcPositions.Select(p => {
	string facility = p["composePosition"]!.GetValue<string>().Split("_")[0];

	if (facility.StartsWith("KZ"))
		return (p, facility[1..]);
	else if (TraconCenters.TryGetValue(facility, out string? artcc))
		return (p, artcc);
	else if (!centerAirports.Any(kvp => kvp.Value.Any(ad => ad.Identifier == facility)))
	{
		if ((p["airportId"] ?? p["centerId"])?.GetValue<string>() is string pos && centerAirports.Any(kvp => kvp.Value.Any(ad => ad.Identifier == pos)))
			return (p, centerAirports.First(kvp => kvp.Value.Any(ad => ad.Identifier == pos)).Key);

		Console.WriteLine(facility);
		return (p, "KZZZ");
	}
	else
		return (p, centerAirports.First(kvp => kvp.Value.Any(ad => ad.Identifier == facility)).Key);
})];

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
includeFolder = Path.Combine(includeFolder, "US");
Directory.CreateDirectory(includeFolder);
string labelFolder = Path.Combine(includeFolder, "labels");
Directory.CreateDirectory(labelFolder);
string geoFolder = Path.Combine(includeFolder, "geos");
Directory.CreateDirectory(geoFolder);
string polygonFolder = Path.Combine(includeFolder, "polygons");
Directory.CreateDirectory(polygonFolder);
string procedureFolder = Path.Combine(includeFolder, "procedures");
Directory.CreateDirectory(procedureFolder);
string navaidFolder = Path.Combine(includeFolder, "navaids");
Directory.CreateDirectory(navaidFolder);
string mvaFolder = Path.Combine(includeFolder, "mvas");
Directory.CreateDirectory(mvaFolder);

Console.Write("Generating shared navigation data...");
File.WriteAllLines(Path.Combine(navaidFolder, "ndb.ndb"), [..cifp.Navaids.SelectMany(kvp => kvp.Value).Where(nv => nv is NDB).Cast<NDB>()
	.Select(ndb => $"{ndb.Identifier} ({ndb.Name});{ndb.Channel};{ndb.Position.Latitude:00.0####};{ndb.Position.Longitude:000.0####};0;")]);
File.WriteAllLines(Path.Combine(navaidFolder, "vor.vor"), [..cifp.Navaids.SelectMany(kvp => kvp.Value).Where(nv => nv is VOR).Cast<VOR>()
	.Select(vor => $"{vor.Identifier} ({vor.Name});{vor.Frequency};{vor.Position.Latitude:00.0####};{vor.Position.Longitude:000.0####};0;")]);
string navaidBlock = "[NDB]\r\nF;ndb.ndb\r\n\r\n[VOR]\r\nF;vor.vor\r\n";
Console.WriteLine(" Done!");

Console.Write("Partitioning airport data...");
Osm apBoundaries = osm.GetFiltered(g =>
	g is Way or Relation &&
	g["aeroway"] == "aerodrome" &&
	g["icao"] is not null &&
	g["abandoned"] is null
);

IDictionary<string, Osm> apOsms = osm.GetFiltered(item => item is not Node n || n["aeroway"] is "parking_position").Group(
	apBoundaries.Ways.Values
		.Concat(apBoundaries.Relations.Values.Where(r => r.Members.Any(m => m is Way))
		.SelectMany(r => r.Members.Where(i => i is Way w).Select(w => (Way)w with { Tags = w.Tags.Append(new("icao", r["icao"]!)).ToFrozenDictionary() })))
		.Select(w => (w["icao"], w))
		.OrderBy(kvp => kvp.w.Tags.ContainsKey("military") ? 1 : 0)
		.DistinctBy(kvp => kvp.Item1)
		.Where(kvp => kvp.Item1 is not null)
		.ToDictionary(kvp => kvp.Item1!, kvp => kvp.w),
	30
);

Dictionary<string, Way[]> artccOsmOnlyIcaos =
	apBoundaries.GetFiltered(apb => !cifp.Aerodromes.ContainsKey(apb["icao"]!)).Group(
		artccBoundaries.ToDictionary(b => b.Key, b => new Way(0, [.. b.Value.Select(n => new Node(0, n.Latitude, n.Longitude, FrozenDictionary<string, string>.Empty))], FrozenDictionary<string, string>.Empty))
	).ToDictionary(
		kvp => kvp.Key,
		kvp => kvp.Value.Ways.Values.Concat(kvp.Value.Relations.Values
			.Where(v => v.Members.Any(m => m is Way))
			.Select(v => (Way)v.Members.MaxBy(m => m is Way w ? w.Nodes.Length : -1)! with { Tags = v.Tags })).ToArray());

Console.WriteLine($" Done!");
Console.Write("Generating labels and centerlines...");

const double CHAR_WIDTH = 0.0001;
foreach (var (icao, apOsm) in apOsms)
{
	List<string> gtsLabels = [];

	// Aprons & Buildings
	foreach (Way location in apOsm.GetFiltered(g => g is Way w && w["aeroway"] is "apron" or "terminal" && (w["name"] ?? w["ref"]) is not null).Ways.Values)
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

	// Geos
	File.WriteAllText(Path.Combine(geoFolder, icao + ".geo"), taxiways.Centerlines + "\r\n\r\n" + gates.Routes);
}

Console.WriteLine($" Done!");
Console.Write("Generating coastline...");
Way[] coastlineGeos = Coastline.LoadTopologies("coastline")['i'];

File.WriteAllLines(
	Path.Combine(geoFolder, "coast.geo"),
	coastlineGeos.Where(w => w.Nodes.Length >= 2 && !w.Nodes.Any(n => n.Latitude < 0 || n.Longitude > 0)).SelectMany(w =>
		w.Nodes.Zip(w.Nodes.Skip(1).Append(w.Nodes[0])).Select(np =>
			$"{np.First.Latitude:00.0####};{np.First.Longitude:000.0####};{np.Second.Latitude:00.0####};{np.Second.Longitude:000.0####};COAST;"
		)
	)
);

Console.WriteLine($" Done!");
Console.Write("Generating polygons...");

var polygonBlocks = apOsms.AsParallel().AsUnordered().Select(input =>
{
	var (icao, apOsm) = input;
	StringBuilder tfls = new();

	// Aprons
	foreach (Way apron in apOsm.GetFiltered(g => g is Way w && w["aeroway"] is "apron").Ways.Values)
	{
		tfls.AppendLine($"STATIC;APRON;1;APRON;");

		foreach (Node n in apron.Nodes.Append(apron.Nodes[0]))
			tfls.AppendLine($"{n.Latitude:00.0#####};{n.Longitude:000.0#####};");
	}

	// Buildings
	foreach (Way building in apOsm.GetFiltered(g => g is Way w && w["aeroway"] is "terminal").Ways.Values)
	{
		tfls.AppendLine($"STATIC;BUILDING;1;BUILDING;");

		foreach (Node n in building.Nodes.Append(building.Nodes[0]))
			tfls.AppendLine($"{n.Latitude:00.0#####};{n.Longitude:000.0#####};");
	}

	// Taxiways
	Taxiways taxiways = new(
		icao,
		apOsm.GetFiltered(g => g is Way w && w["aeroway"] is "taxiway" or "taxilane")
	);
	foreach (Way txw in taxiways.BoundingBoxes)
	{
		tfls.AppendLine($"STATIC;TAXIWAY;1;TAXIWAY;");

		foreach (Node n in txw.Nodes)
			tfls.AppendLine($"{n.Latitude:00.0#####};{n.Longitude:000.0#####};");
	}

	double rwWidth = cifp.Runways.TryGetValue(icao, out var rws) ? rws.Average(rw => rw.Width * 0.00000137) : 0.0002;
	// Runways
	foreach (Way rw in apOsm.GetFiltered(g => g is Way w && w["aeroway"] is "runway").Ways.Select(rw => rw.Value.Inflate(rwWidth)))
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
Console.Write("Generating procedures...");
ConcurrentDictionary<string, HashSet<NamedCoordinate>> apProcFixes = [];
Procedures procs = new(cifp);

Parallel.ForEach(cifp.Aerodromes.Values, airport =>
{
	var (sidLines, sidFixes) = procs.AirportSidLines(airport.Identifier);
	var (starLines, starFixes) = procs.AirportStarLines(airport.Identifier);
	var (iapLines, iapFixes) = procs.AirportApproachLines(airport.Identifier);

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

Console.WriteLine($" Done!");
SemaphoreSlim mrvaWrites = new(1);

Parallel.ForEach(faaArtccs, async artcc =>
{

	Airport[] ifrAirports = [.. centerAirports[artcc].Where(ad => ad is Airport ap && ap.IFR).Cast<Airport>()];

	if (ifrAirports.Length == 0)
	{
		Console.WriteLine($"{artcc} skipped.");
		return;
	}

	(double Latitude, double Longitude) centerpoint = (
		ifrAirports.Average(ap => (double)ap.Location.Latitude),
		ifrAirports.Average(ap => (double)ap.Location.Longitude)
	);

	// Info.
	double cosLat = Math.Cos(centerpoint.Latitude * Math.PI / 180);

	string infoBlock = $@"[INFO]
{DMS(centerpoint.Latitude, false)}
{DMS(centerpoint.Longitude, true)}
60
{60 * Math.Abs(cosLat):00}
{ifrAirports.Average(ap => ap.MagneticVariation):00.0000}
US/{artcc};US/labels;US/geos;US/polygons;US/procedures;US/navaids;US/mvas
";
	string artccFolder = Path.Combine(includeFolder, artcc);
	if (!Directory.Exists(artccFolder))
		Directory.CreateDirectory(artccFolder);

	// Colours.
	string defineBlock = $@"[DEFINE]
TAXIWAY;#999A99;
APRON;#B9BBBB;
OUTLINE;#000000;
BUILDING;#773333;
RUNWAY;#555555;
STOPBAR;#B30000;
";

	// ATC Positions.
	string atcBlock = "[ATC]\r\nF;atc.atc\r\n";
	File.WriteAllLines(Path.Combine(artccFolder, "atc.atc"), [..
		positionArtccs.Where(p => p.Artcc == artcc).Select(p => $"{p.Position["composePosition"]!.GetValue<string>()};{p.Position["frequency"]!.GetValue<decimal>()};")
	]);

	// Airports (main).
	string airportBlock = "[AIRPORT]\r\nF;airports.ap\r\n";
	File.WriteAllLines(Path.Combine(artccFolder, "airports.ap"), [..
		centerAirports[artcc].Select(ad => $"{ad.Identifier};{ad.Elevation.ToMSL().Feet};18000;{ad.Location.Latitude:00.0####};{ad.Location.Longitude:000.0####};{ad.Name.TrimEnd()};")
		.Concat(artccOsmOnlyIcaos.TryGetValue(artcc, out var aooi) ? aooi.Select(w => $"{w["icao"]!};0;18000;{w.Nodes.Average(n => n.Latitude):00.0####};{w.Nodes.Average(n => n.Longitude):000.0####};{w["name"] ?? "Unknown Airport"};") : [])
	]);

	// Runways.
	string runwayBlock = "[RUNWAY]\r\nF;runways.rw\r\n";
	File.WriteAllText(Path.Combine(artccFolder, "runways.rw"), string.Join(
	"\r\n",
	centerRunways[artcc].SelectMany(crg =>
		crg.Runways
			.Where(rw => rw.Identifier.CompareTo(rw.OppositeIdentifier) <= 0 && crg.Runways.Any(rw2 => rw.OppositeIdentifier == rw2.Identifier))
			.Select(rw => (Primary: rw, Opposite: crg.Runways.First(rw2 => rw2.Identifier == rw.OppositeIdentifier)))
			.Select(rws => $"{crg.Airport};{rws.Primary.Identifier};{rws.Opposite.Identifier};{rws.Primary.TDZE.ToMSL().Feet};{rws.Opposite.TDZE.ToMSL().Feet};{(int)rws.Primary.Course.Degrees};{(int)rws.Opposite.Course.Degrees};" +
						   $"{rws.Primary.Endpoint.Latitude:00.0####};{rws.Primary.Endpoint.Longitude:000.0####};{rws.Opposite.Endpoint.Latitude:00.0####};{rws.Opposite.Endpoint.Longitude:000.0####};")
	)
));

	// Airways.
	Airway[] inScopeLowAirways = [.. cifp.Airways.Where(kvp => kvp.Key[0] is 'V' or 'T').SelectMany(kvp => kvp.Value.Where(v => v.Count() >= 2 && v.Any(p => IsInPolygon(artccBoundaries[artcc], ((double)p.Point.Latitude, (double)p.Point.Longitude)))))];
	Airway[] inScopeHighAirways = [.. cifp.Airways.Where(kvp => kvp.Key[0] is 'Q' or 'J').SelectMany(kvp => kvp.Value.Where(v => v.Count() >= 2 && v.Any(p => IsInPolygon(artccBoundaries[artcc], ((double)p.Point.Latitude, (double)p.Point.Longitude)))))];
	string airwaysBlock = $@"[LOW AIRWAY]
F;airways.low

[HIGH AIRWAY]
F;airways.high
";

	File.WriteAllLines(Path.Combine(artccFolder, "airways.low"), inScopeLowAirways.SelectMany(v => (string[])[
		$"L;{v.Identifier};{v.Skip(v.Count() / 2).First().Point.Latitude:00.0####};{v.Skip(v.Count() / 2).First().Point.Longitude:000.0####};",
		..v.Select(p => $"T;{v.Identifier};{p.Name ?? p.Point.Latitude.ToString("00.0####")};{p.Name ?? p.Point.Longitude.ToString("000.0####")};")
	]));

	File.WriteAllLines(Path.Combine(artccFolder, "airways.high"), inScopeHighAirways.SelectMany(v => (string[])[
		$"L;{v.Identifier};{v.Skip(v.Count() / 2).First().Point.Latitude:00.0####};{v.Skip(v.Count() / 2).First().Point.Longitude:000.0####};",
		..v.Select(p => $"T;{v.Identifier};{p.Name ?? p.Point.Latitude.ToString("00.0####")};{p.Name ?? p.Point.Longitude.ToString("000.0####")};")
	]));

	// Fixes.
	string fixesBlock = "[FIXES]\r\nF;fixes.fix\r\n";
	(string Key, Coordinate Point)[] fixes = [..cifp.Fixes.SelectMany(g => g.Value.Select(v => (g.Key, Point: v.GetCoordinate())))
		.Where(f => IsInPolygon(artccBoundaries[artcc], ((double)f.Point.Latitude, (double)f.Point.Longitude)))
		.Concat(cifp.Navaids.SelectMany(g => g.Value.Select(v => (g.Key, Point: v.Position))))];

	File.WriteAllLines(Path.Combine(artccFolder, "fixes.fix"), [..
		fixes
			.Concat(inScopeLowAirways.SelectMany(aw => aw.Where(p => p.Name is string n && !fixes.Any(f => f.Key == n))).Select(p => (Key: p.Name!, Point: p.Point.GetCoordinate())))
			.Concat(inScopeHighAirways.SelectMany(aw => aw.Where(p => p.Name is string n && !fixes.Any(f => f.Key == n))).Select(p => (Key: p.Name!, Point: p.Point.GetCoordinate())))
			.Concat(centerAirports[artcc].SelectMany(icao => apProcFixes.TryGetValue(icao.Identifier, out var fixes) ? fixes : []).Select(p => (Key: p.Name!, Point: p.GetCoordinate())))
			.Select(f => $"{f.Key};{f.Point.Latitude:00.0####};{f.Point.Longitude:000.0####};")
	]);

	// ARTCC boundaries.
	// TODO: High boundaries for TRACONs.
	string artccBlock = $@"[ARTCC]
F;artcc.artcc

[ARTCC LOW]
F;low.artcc
";

	File.WriteAllText(Path.Combine(artccFolder, "artcc.artcc"), $@"{string.Join("\r\n", artccBoundaries[artcc].Append(artccBoundaries[artcc][0]).Select(bp => $"T;{artcc};{bp.Latitude:00.0####};{bp.Longitude:000.0####};"))}
{string.Join("\r\n",
	artccNeighbours[artcc].Select(n => string.Join(
		"\r\n",
		artccBoundaries[n].Zip(artccBoundaries[n][1..].Append(artccBoundaries[n][0])).Append((First: artccBoundaries[n][0], Second: artccBoundaries[n][0]))
			.Select(bps =>
				new Coordinate((decimal)bps.First.Latitude, (decimal)bps.First.Longitude).DistanceTo(new((decimal)bps.Second.Latitude, (decimal)bps.Second.Longitude)) < 500
				? $"T;{n};{bps.First.Latitude:00.0####};{bps.First.Longitude:000.0####};"
				: "T;Dummy;N000.00.00.000;W000.00.00.000;")
			.Prepend($"L;{n};{artccBoundaries[n].Average(bp => bp.Latitude):00.0####};{artccBoundaries[n].Average(bp => bp.Longitude):000.0####};7;")
	))
)}
");

	AirspaceDrawing ad = new(cifp.Airspaces.Where(ap => ap.Regions.Any(r => r.Class is ControlledAirspace.AirspaceClass.B && r.Boundaries.Any(b => IsInPolygon(artccBoundaries[artcc], ((double)b.Vertex.Latitude, (double)b.Vertex.Longitude))))));
	File.WriteAllText(Path.Combine(artccFolder, "low.artcc"), ad.ClassBPaths);

	// TODO: VFR Routes
	string vfrBlock = "[VFRFIX]\r\nF;vfr.fix\r\n";

	File.WriteAllLines(Path.Combine(artccFolder, "vfr.fix"), [..
		fixes
			.Where(f => f.Key.StartsWith("VP"))
			.Select(f => $"{f.Key};{f.Point.Latitude:00.0####};{f.Point.Longitude:000.0####};")
	]);

	// MRVAs
	Mrva mrvas = new(artccBoundaries[artcc]);
	string mvaBlock = $@"[MVA]
{string.Join("\r\n", mrvas.Volumes.Keys.Select(k => "F;K" + k + ".mva"))}
";

	string genLabelLine(string volume, Mrva.MrvaSegment seg)
	{
		double lat = seg.BoundaryPoints.Average(bp => bp.Latitude),
			   lon = seg.BoundaryPoints.Average(bp => bp.Longitude);

		if (mrvas.Volumes[volume].Count(s => IsInPolygon(s.BoundaryPoints, (lat, lon))) > 1)
			// Ambiguous. Skip it!
			return "";

		return $"L;{seg.Name};{lat:00.0####};{lon:000.0####};{seg.MinimumAltitude / 100:000};8;";
	}

	foreach (var (fn, volume) in mrvas.Volumes)
	{
		await mrvaWrites.WaitAsync();
		if (!File.Exists(Path.Combine(mvaFolder, "K" + fn + ".mva")))
			File.WriteAllLines(Path.Combine(mvaFolder, "K" + fn + ".mva"), volume.Select(seg => string.Join("\r\n",
				seg.BoundaryPoints.Select(bp => $"T;{seg.Name};{bp.Latitude:00.0####};{bp.Longitude:000.0####};")
								  .Prepend(genLabelLine(fn, seg))
			)));
		mrvaWrites.Release();
	}

	// Airports (additional).
	File.AppendAllLines(Path.Combine(artccFolder, "airports.ap"), [..
		mrvas.Volumes.Keys
			.Where(k => !centerAirports[artcc].Any(ad => ad.Identifier == "K" + k)).Select(k =>
			$"K{k};{mrvas.Volumes[k].Min(s => s.MinimumAltitude)};18000;" +
			$"{mrvas.Volumes[k].Average(s => s.BoundaryPoints.Average(bp => bp.Latitude)):00.0####};{mrvas.Volumes[k].Average(s => s.BoundaryPoints.Average(bp => bp.Longitude)):000.0####};" +
			$"{k} TRACON;"
		)
	]);

	// Geo file references.
	string geoBlock = @$"[GEO]
F;coast.geo
{string.Join("\r\n", centerAirports[artcc].Select(ap => $"F;{ap.Identifier}.geo"))}
{(artccOsmOnlyIcaos.TryGetValue(artcc, out var aoois) ? string.Join("\r\n", aoois.Select(ap => $"F;{ap}.geo")) : "")}
";

	File.WriteAllText(Path.Combine(config.OutputFolder, $"K{artcc.ToUpperInvariant()}.isc"), $@"{infoBlock}
{defineBlock}
{atcBlock}
{airportBlock}
{runwayBlock}
{fixesBlock}
{navaidBlock}
{airwaysBlock}
{vfrBlock}
{mvaBlock}
{artccBlock}
{geoBlock}");

	Console.Write($"{artcc} ");
});