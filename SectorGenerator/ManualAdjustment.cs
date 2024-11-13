using CIFPReader;

using System.Text.RegularExpressions;

namespace SectorGenerator;
internal abstract partial record ManualAdjustment
{
	public static IEnumerable<ManualAdjustment> Process(string filecontents)
	{
		HashSet<NamedCoordinate> generatedOrphanFixes = [];

		filecontents = filecontents.ReplaceLineEndings("\n");
		bool NewlinePending() => filecontents.TakeWhile(char.IsWhiteSpace).Contains('\n');

		int GetIndentLevel()
		{
			while (NewlinePending())
				filecontents = filecontents[(filecontents.IndexOf('\n') + 1)..];

			string indent = new([.. filecontents.TakeWhile(c => char.IsWhiteSpace(c) && c != '\n')]);
			filecontents = filecontents[indent.Length..];

			if (filecontents.Length == 0 || filecontents.All(char.IsWhiteSpace))
				return 0;

			int retval = 0;
			foreach (char c in indent)
				retval += c switch {
					' ' => 1,
					'\t' => 4 - (retval % 4),
					_ => 0
				};

			return retval;
		}

		for (int indent = GetIndentLevel(); filecontents.Length > 0;)
		{
			string header = new string([.. filecontents.TakeWhile(char.IsLetter)]).ToUpperInvariant();
			filecontents = filecontents[header.Length..].TrimStart();

			void AbsorbBlock(int startingIndent)
			{
				filecontents = filecontents[filecontents.TakeWhile(c => c != '\n').Count()..];
				while ((indent = GetIndentLevel()) > startingIndent) ;
			}

			PossiblyResolvedWaypoint? ReadWaypoint(string context)
			{
				Match match = PossibleWaypointRegex().Match(filecontents);
				filecontents = filecontents[match.Length..].TrimStart([' ', '\t']);
				if (!match.Success || match.Length == 0)
				{
					Console.WriteLine($"ERROR! Invalid waypoint {match.Value} {filecontents.Split()[0]} in {context}.");
					return null;
				}

				string? fixName = null;
				if (match.Groups.TryGetValue("fix", out var fixNameGroup) && fixNameGroup.Success)
					fixName = fixNameGroup.Value;

				Coordinate? fixCoord = null;
				if (match.Groups.TryGetValue("lat", out var fixLatGroup) && fixLatGroup.Success && match.Groups.TryGetValue("lon", out var fixLonGroup) && fixLonGroup.Success)
					fixCoord = new(
						decimal.Parse(fixLatGroup.Value.TrimEnd(['N', 'n', 'S', 's'])) * (fixLatGroup.Value[^1] is 'S' or 's' ? -1 : 1),
						decimal.Parse(fixLonGroup.Value.TrimEnd(['E', 'e', 'W', 'w'])) * (fixLonGroup.Value[^1] is 'W' or 'w' ? -1 : 1)
					);

				uint? radial = null;
				decimal? distance = null;
				if (match.Groups.TryGetValue("radial", out var radialGroup) && radialGroup.Success && match.Groups.TryGetValue("distance", out var distanceGroup) && distanceGroup.Success)
					(radial, distance) = (uint.Parse(radialGroup.Value), decimal.Parse(distanceGroup.Value));

				if (fixName is string ofName && fixCoord is Coordinate ofCoord)
					generatedOrphanFixes.Add(new(ofName, ofCoord));

				if (fixCoord is Coordinate rCoord && radial is uint rRad && distance is decimal rDist)
					// Fix radial distance from known point; e.g. (12.3, -123.4)123@4.5
					return new PossiblyResolvedWaypoint(rCoord.FixRadialDistance(new TrueCourse(rRad), rDist), null, null);
				else if (fixName is string urFix && radial is uint urRad && distance is decimal urDist)
					// Fix radial distance from unknown point; e.g. COORD123@4.5
					return new PossiblyResolvedWaypoint(null, null, new(new(urFix), new MagneticCourse(urRad, null), urDist));
				else if (fixName is string pFix && fixCoord is Coordinate pCoord)
					// Named coordinate; e.g. COORD(12.3, -123.4)
					return new PossiblyResolvedWaypoint(new NamedCoordinate(pFix, pCoord), null, null);
				else if (fixName is string fix)
					// Undefined fix; e.g. COORD
					return new PossiblyResolvedWaypoint(null, new(fix), null);
				else if (fixCoord is Coordinate coord)
					// Raw lat/lon; e.g. (12.3, -123.4)
					return new PossiblyResolvedWaypoint(coord, null, null);
				else throw new NotImplementedException();
			}

			IDrawableGeo? ReadGeo(string context)
			{
				string geoType = new string([.. filecontents.TakeWhile(char.IsLetter)]).ToUpperInvariant();
				filecontents = filecontents[geoType.Length..].TrimStart([' ', '\t']);

				decimal GetSize(decimal @default)
				{
					Match m = SizeRegex().Match(filecontents);

					if (!m.Success)
						return @default;

					filecontents = filecontents[m.Length..].TrimStart([' ', '\t']);

					return decimal.Parse(m.Groups["size"].Value);
				}

				switch (geoType)
				{
					case "CIRCLE":
						decimal circleSize = GetSize(0.5m);
						if (ReadWaypoint($"centerpoint of circle in {context}") is not PossiblyResolvedWaypoint circleCenter)
							return null;

						return new GeoSymbol.Circle(circleCenter, circleSize);

					case "WAYPOINT":
						decimal wpSize = GetSize(1m);
						if (ReadWaypoint($"centerpoint of waypoint in {context}") is not PossiblyResolvedWaypoint wpCenter)
							return null;

						return new GeoSymbol.Waypoint(wpCenter, wpSize);

					case "TRIANGLE":
						decimal triSize = GetSize(0.5m);
						if (ReadWaypoint($"centerpoint of triangle in {context}") is not PossiblyResolvedWaypoint triCenter)
							return null;

						return new GeoSymbol.Triangle(triCenter, triSize);

					case "NUCLEAR":
						decimal nukeSize = GetSize(1m);
						if (ReadWaypoint($"centerpoint of nuclear in {context}") is not PossiblyResolvedWaypoint nukeCenter)
							return null;

						return new GeoSymbol.Nuclear(nukeCenter, nukeSize);

					case "FLAG":
						decimal flagSize = GetSize(1m);
						if (ReadWaypoint($"centerpoint of flag in {context}") is not PossiblyResolvedWaypoint flagCenter)
							return null;

						return new GeoSymbol.Flag(flagCenter, flagSize);

					case "DIAMOND":
						decimal diamondSize = GetSize(1m);
						if (ReadWaypoint($"centerpoint of diamond in {context}") is not PossiblyResolvedWaypoint diamondCenter)
							return null;

						return new GeoSymbol.Diamond(diamondCenter, diamondSize);

					case "CHEVRON":
						decimal chevronSize = GetSize(0.25m);
						if (ReadWaypoint($"centerpoint of chevron in {context}") is not PossiblyResolvedWaypoint chevronCenter)
							return null;

						return new GeoSymbol.Chevron(chevronCenter, chevronSize);

					case "BOX":
						decimal boxSize = GetSize(0.5m);
						if (ReadWaypoint($"centerpoint of box in {context}") is not PossiblyResolvedWaypoint boxCenter)
							return null;

						return new GeoSymbol.Box(boxCenter, boxSize);

					case "STAR":
						decimal starSize = GetSize(1m);
						if (ReadWaypoint($"centerpoint of star in {context}") is not PossiblyResolvedWaypoint starCenter)
							return null;

						return new GeoSymbol.Star(starCenter, starSize);

					case "LINE":
						List<PossiblyResolvedWaypoint> lineWaypoints = [];

						while (filecontents.Length > 0 && !NewlinePending() && ReadWaypoint($"point on line in {context}") is PossiblyResolvedWaypoint lwp)
							lineWaypoints.Add(lwp);

						if (filecontents.Length > 0 && !NewlinePending())
							return null;

						return new GeoConnector.Line([.. lineWaypoints]);

					case "DASH":
						decimal dashSize = GetSize(1m);
						List<PossiblyResolvedWaypoint> dashWaypoints = [];

						while (filecontents.Length > 0 && !NewlinePending() && ReadWaypoint($"point on dash in {context}") is PossiblyResolvedWaypoint dwp)
							dashWaypoints.Add(dwp);

						if (filecontents.Length > 0 && !NewlinePending())
							return null;

						return new GeoConnector.Dash([.. dashWaypoints], dashSize);

					case "ARROW":
						decimal arrowSize = GetSize(0.25m);
						List<PossiblyResolvedWaypoint> arrowWaypoints = [];

						while (filecontents.Length > 0 && !NewlinePending() && ReadWaypoint($"point on arrow in {context}") is PossiblyResolvedWaypoint awp)
							arrowWaypoints.Add(awp);

						if (filecontents.Length > 0 && !NewlinePending())
							return null;

						return new GeoConnector.Arrow([.. arrowWaypoints], arrowSize);

					default:
						Console.WriteLine($"ERROR! Unknown geo type {geoType} in {context}.");
						filecontents = filecontents[filecontents.TakeWhile(c => c != '\n').Count()..];
						return null;
				}
			}

			switch (header)
			{
				case "FIX":
					string fixname = new string([.. filecontents.TakeWhile(c => c != ':' && !char.IsWhiteSpace(c))]).ToUpperInvariant();
					filecontents = filecontents[fixname.Length..].TrimStart();

					if (filecontents[0] != ':')
					{
						Console.WriteLine($"ERROR! Expected : in fix definition after FIX {fixname}.");
						AbsorbBlock(indent);
						continue;
					}

					filecontents = filecontents[1..].TrimStart([' ', '\t']);
					if (NewlinePending())
					{
						Console.WriteLine($"ERROR! Expected a definition for FIX {fixname}.");
						AbsorbBlock(indent);
						continue;
					}

					if (((string[])["DEL", "DELETE", "REM", "REMOVE"]).Contains(new string([.. filecontents.TakeWhile(char.IsLetter)]).ToUpperInvariant()))
					{
						yield return new RemoveFix(new(null, new(fixname), null));
						filecontents = filecontents[filecontents.TakeWhile(char.IsLetter).Count()..].TrimStart([' ', '\t']);
						break;
					}

					if (ReadWaypoint($"definition for FIX {fixname}") is not PossiblyResolvedWaypoint fwp)
					{
						AbsorbBlock(indent);
						continue;
					}

					yield return new AddFix(fixname, fwp);
					break;

				case "VFRFIX":
					string vfrfixname = new string([.. filecontents.TakeWhile(c => c != ':' && !char.IsWhiteSpace(c))]).ToUpperInvariant();
					filecontents = filecontents[vfrfixname.Length..].TrimStart();

					if (filecontents[0] != ':')
					{
						Console.WriteLine($"ERROR! Expected : in fix definition after VFRFIX {vfrfixname}.");
						AbsorbBlock(indent);
						continue;
					}

					filecontents = filecontents[1..].TrimStart([' ', '\t']);
					if (NewlinePending())
					{
						Console.WriteLine($"ERROR! Expected a definition for VFRFIX {vfrfixname}.");
						AbsorbBlock(indent);
						continue;
					}

					if (ReadWaypoint($"definition for VFRFIX {vfrfixname}") is not PossiblyResolvedWaypoint prvfwp)
					{
						AbsorbBlock(indent);
						continue;
					}

					yield return new AddVfrFix(vfrfixname, prvfwp);
					break;

				case "VFRROUTE":
					if (filecontents[0] != ':')
					{
						Console.WriteLine("ERROR! Expected : in fix definition after VFRROUTE.");
						AbsorbBlock(indent);
						continue;
					}
					filecontents = filecontents[1..].TrimStart([' ', '\t']);

					bool blockFormat = NewlinePending();
					int blockIndent = blockFormat ? GetIndentLevel() : 0;
					if (blockFormat && blockIndent <= indent)
					{
						indent = blockIndent;
						Console.WriteLine("ERROR! Expected definition for VFRROUTE.");
						AbsorbBlock(indent);
						continue;
					}

					List<PossiblyResolvedWaypoint> vfrRouteWaypoints = [];

					if (blockFormat)
						filecontents = new string(' ', blockIndent) + filecontents; // Reset for the next indent check.

					int runningIndent = blockIndent;
					while (filecontents.Any(c => !char.IsWhiteSpace(c)) && ((blockFormat && (runningIndent = GetIndentLevel()) >= blockIndent) || (!blockFormat && !NewlinePending())))
					{
						if (ReadWaypoint("VFRROUTE") is not PossiblyResolvedWaypoint prw)
							// Skip it.
							continue;

						vfrRouteWaypoints.Add(prw);
					}

					yield return new AddVfrRoute([.. vfrRouteWaypoints]);
					break;

				case "GEO":
					string geoTag = new string([.. filecontents.TakeWhile(c => c != ':' && !char.IsWhiteSpace(c))]).ToUpperInvariant();
					filecontents = filecontents[geoTag.Length..].TrimStart();

					if (filecontents[0] != ':')
					{
						Console.WriteLine($"ERROR! Expected : in geo definition after GEO {geoTag}.");
						AbsorbBlock(indent);
						continue;
					}

					filecontents = filecontents[1..].TrimStart([' ', '\t']);
					int geoIndentDepth;
					if (!NewlinePending() || (geoIndentDepth = GetIndentLevel()) <= indent)
					{
						Console.WriteLine($"ERROR! Expected definition block for GEO {geoTag}.");
						AbsorbBlock(indent);
						continue;
					}

					List<IDrawableGeo> geos = [];
					do
					{
						if (ReadGeo($"GEO {geoTag}") is IDrawableGeo geo)
							geos.Add(geo);
						else
							filecontents = filecontents[filecontents.TakeWhile(c => c != '\n').Count()..];
					} while ((geoIndentDepth = GetIndentLevel()) > indent);

					indent = geoIndentDepth;
					yield return new AddGeo(geoTag, [.. geos]);
					continue;

				default:
					Console.WriteLine($"ERROR! Unrecognised header {header}.");
					AbsorbBlock(indent);
					continue;
			}

			int nextIndentLevel = GetIndentLevel();
			if (nextIndentLevel > indent)
			{
				Console.WriteLine("Unexpected extra indent! Eating block.");
				AbsorbBlock(indent);
				continue;
			}

			indent = nextIndentLevel;
		}

		foreach (NamedCoordinate nc in generatedOrphanFixes)
			yield return new AddFix(nc.Name, new(nc, null, null));
	}

	[GeneratedRegex(@"\A(?<fix>([A-Z0-9/](?!..@))+)?(\((?<lat>[-+]?\d+(\.\d+)?[NS]?)\s*[^A-Z0-9]\s*(?<lon>[-+]?\d+(\.\d+)?[EW]?)\))?((?<radial>\d{3})@(?<distance>\d+(\.\d+)?))?", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture)]
	public static partial Regex PossibleWaypointRegex();

	[GeneratedRegex(@"\A[^\S\n]*\([^\S\n]*(?<size>[+-]?\d+(\.\d+)?)[^\S\n]*\)[^\S\n]*", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture)]
	public static partial Regex SizeRegex();
}

internal abstract record ManualAddition : ManualAdjustment;

internal abstract record ManualDeletion : ManualAdjustment;

internal record AddFix(string Name, PossiblyResolvedWaypoint Position) : ManualAddition;
internal sealed record AddVfrFix(string Name, PossiblyResolvedWaypoint Position) : AddFix(Name, Position);
internal sealed record RemoveFix(PossiblyResolvedWaypoint Fix) : ManualDeletion;

internal sealed record AddVfrRoute(PossiblyResolvedWaypoint[] Points) : ManualAddition;

internal sealed record AddGeo(string Tag, IDrawableGeo[] Geos) : ManualAddition;

internal record struct PossiblyResolvedWaypoint(ICoordinate? Coordinate, UnresolvedWaypoint? FixName, UnresolvedFixRadialDistance? FixRadialDistance)
{
	public readonly ICoordinate Resolve(CIFP cifp)
	{
		if (Coordinate is ICoordinate cic) return cic;

		if (FixName is UnresolvedWaypoint fnuw) return fnuw.Resolve(cifp.Fixes);

		if (FixRadialDistance is UnresolvedFixRadialDistance frdud) return frdud.Resolve(cifp.Fixes, cifp.Navaids);

		throw new NotImplementedException();
	}
}

internal interface IDrawableGeo
{
	public bool Resolve(CIFP cifp);
	public IEnumerable<Coordinate?> Draw();
	public Coordinate[] ReferencePoints { get; }
}

internal abstract record GeoSymbol(PossiblyResolvedWaypoint Centerpoint, decimal Size) : IDrawableGeo
{
	protected Coordinate _resolvedCenterpoint = new(0, 0);
	protected decimal _magVar = 0;

	public bool Resolve(CIFP cifp)
	{
		try
		{
			_resolvedCenterpoint = Centerpoint.Resolve(cifp).GetCoordinate();
			_magVar = cifp.Navaids.GetLocalMagneticVariation(_resolvedCenterpoint).Variation;
			return true;
		}
		catch { return false; }
	}

	public abstract IEnumerable<Coordinate?> Draw();

	public Coordinate[] ReferencePoints => [_resolvedCenterpoint];

	public sealed record Circle(PossiblyResolvedWaypoint Centerpoint, decimal Size) : GeoSymbol(Centerpoint, Size)
	{
		public override IEnumerable<Coordinate?> Draw() =>
			Enumerable.Range(0, 37)
			.Select(r => new TrueCourse(r * 10))
			.Select(r => (Coordinate?)_resolvedCenterpoint.FixRadialDistance(r, Size));
	}

	public sealed record Waypoint(PossiblyResolvedWaypoint Centerpoint, decimal Size) : GeoSymbol(Centerpoint, Size)
	{
		const decimal INNER_SCALE = 0.35m;
		const decimal FLARE_1_SCALE = 0.4m;
		const decimal FLARE_2_SCALE = 0.5m;

		private IEnumerable<Coordinate?> RepeatFirst(IEnumerable<Coordinate?> source)
		{
			Coordinate? first = null;
			foreach (Coordinate? c in source)
			{
				first ??= c;
				yield return c;
			}

			yield return first;
		}

		public override IEnumerable<Coordinate?> Draw() => [
			..Enumerable.Range(0, 37).Select(r => new TrueCourse(r * 10)).Select(r => _resolvedCenterpoint.FixRadialDistance(r, Size * INNER_SCALE)), null,
			..RepeatFirst(Enumerable.Range(1, 4).Select(r => new MagneticCourse(r * 90, _magVar)).SelectMany<MagneticCourse, Coordinate?>(r => [
				_resolvedCenterpoint.FixRadialDistance(r - 45, Size * INNER_SCALE),
				_resolvedCenterpoint.FixRadialDistance(r - 30, Size * FLARE_1_SCALE),
				_resolvedCenterpoint.FixRadialDistance(r - 15, Size * FLARE_2_SCALE),
				_resolvedCenterpoint.FixRadialDistance(r, Size),
				_resolvedCenterpoint.FixRadialDistance(r + 15, Size * FLARE_2_SCALE),
				_resolvedCenterpoint.FixRadialDistance(r + 30, Size * FLARE_1_SCALE),
			]))
		];
	}

	public sealed record Triangle(PossiblyResolvedWaypoint Centerpoint, decimal Size) : GeoSymbol(Centerpoint, Size)
	{
		public override IEnumerable<Coordinate?> Draw() => [
			_resolvedCenterpoint.FixRadialDistance(new MagneticCourse(000m, _magVar), Size),
			_resolvedCenterpoint.FixRadialDistance(new MagneticCourse(120m, _magVar), Size),
			_resolvedCenterpoint.FixRadialDistance(new MagneticCourse(240m, _magVar), Size),
			_resolvedCenterpoint.FixRadialDistance(new MagneticCourse(000m, _magVar), Size),
		];
	}

	public sealed record Nuclear(PossiblyResolvedWaypoint Centerpoint, decimal Size) : GeoSymbol(Centerpoint, Size)
	{
		const decimal INNER_SCALE = 0.15m;

		public override IEnumerable<Coordinate?> Draw()
		{
			bool far = true;

			for (uint angle = 0; angle <= 360; angle += 10)
			{
				MagneticCourse radial = new(angle, _magVar);
				yield return _resolvedCenterpoint.FixRadialDistance(radial, far ? Size : Size * INNER_SCALE);

				if ((angle + 30) % 60 == 0)
				{
					far = !far;
					yield return _resolvedCenterpoint.FixRadialDistance(radial, far ? Size : Size * INNER_SCALE);
				}
			}
		}
	}

	public sealed record Flag(PossiblyResolvedWaypoint Centerpoint, decimal Size) : GeoSymbol(Centerpoint, Size)
	{
		public override IEnumerable<Coordinate?> Draw() => [
			_resolvedCenterpoint,
			_resolvedCenterpoint.FixRadialDistance(new MagneticCourse(00, _magVar), Size),
			_resolvedCenterpoint.FixRadialDistance(new MagneticCourse(30, _magVar), Size),
			_resolvedCenterpoint.FixRadialDistance(new MagneticCourse(00, _magVar), Size * 0.66m),
		];
	}

	public sealed record Diamond(PossiblyResolvedWaypoint Centerpoint, decimal Size) : GeoSymbol(Centerpoint, Size)
	{
		public override IEnumerable<Coordinate?> Draw() => [
			_resolvedCenterpoint.FixRadialDistance(new MagneticCourse(000m, _magVar), Size),
			_resolvedCenterpoint.FixRadialDistance(new MagneticCourse(090m, _magVar), Size * 0.5m),
			_resolvedCenterpoint.FixRadialDistance(new MagneticCourse(180m, _magVar), Size),
			_resolvedCenterpoint.FixRadialDistance(new MagneticCourse(270m, _magVar), Size * 0.5m),
			_resolvedCenterpoint.FixRadialDistance(new MagneticCourse(000m, _magVar), Size),
		];
	}

	public sealed record Chevron(PossiblyResolvedWaypoint Centerpoint, decimal Size) : GeoSymbol(Centerpoint, Size)
	{
		public override IEnumerable<Coordinate?> Draw() => [
			_resolvedCenterpoint.FixRadialDistance(new MagneticCourse(240m, _magVar), Size),
			_resolvedCenterpoint.FixRadialDistance(new MagneticCourse(000m, _magVar), Size),
			_resolvedCenterpoint.FixRadialDistance(new MagneticCourse(120m, _magVar), Size),
		];
	}

	public sealed record Box(PossiblyResolvedWaypoint Centerpoint, decimal Size) : GeoSymbol(Centerpoint, Size)
	{
		public override IEnumerable<Coordinate?> Draw() => [
			_resolvedCenterpoint.FixRadialDistance(new MagneticCourse(045m, _magVar), Size),
			_resolvedCenterpoint.FixRadialDistance(new MagneticCourse(135m, _magVar), Size),
			_resolvedCenterpoint.FixRadialDistance(new MagneticCourse(225m, _magVar), Size),
			_resolvedCenterpoint.FixRadialDistance(new MagneticCourse(315m, _magVar), Size),
			_resolvedCenterpoint.FixRadialDistance(new MagneticCourse(045m, _magVar), Size),
		];
	}

	public sealed record Star(PossiblyResolvedWaypoint Centerpoint, decimal Size) : GeoSymbol(Centerpoint, Size)
	{
		const decimal INNER_SCALE = 0.5m;
		public override IEnumerable<Coordinate?> Draw() =>
			Enumerable.Range(0, 11)
			.Select(r => new MagneticCourse(r * 36, _magVar))
			.Select(r => (Coordinate?)_resolvedCenterpoint.FixRadialDistance(r, (int)r.Degrees % 72 == 0 ? Size : Size * INNER_SCALE));
	}
}

internal abstract record GeoConnector(PossiblyResolvedWaypoint[] Points) : IDrawableGeo
{
	protected Coordinate[] _resolvedPoints = [];

	public bool Resolve(CIFP cifp)
	{
		try
		{
			_resolvedPoints = [.. Points.Select(p => p.Resolve(cifp).GetCoordinate())];
			return true;
		}
		catch { return false; }
	}

	public abstract IEnumerable<Coordinate?> Draw();

	public Coordinate[] ReferencePoints => _resolvedPoints;

	public sealed record Line(PossiblyResolvedWaypoint[] Points) : GeoConnector(Points)
	{
		public override IEnumerable<Coordinate?> Draw() => _resolvedPoints.Cast<Coordinate?>();
	}

	/// <param name="Size">STARS default 1nmi.</param>
	public sealed record Dash(PossiblyResolvedWaypoint[] Points, decimal Size) : GeoConnector(Points)
	{
		public override IEnumerable<Coordinate?> Draw()
		{
			if (_resolvedPoints.Length < 2)
				yield break;

			bool lastReturned = false;
			foreach (var (from, to) in _resolvedPoints[..^1].Zip(_resolvedPoints[1..]))
			{
				if (from.GetBearingDistance(to).bearing is not TrueCourse direction) continue;
				Coordinate next;

				for (Coordinate startPoint = from; startPoint.DistanceTo(to) > Size; startPoint = next)
				{
					next = startPoint.FixRadialDistance(direction, Math.Min(Size, startPoint.DistanceTo(to)));

					if (lastReturned)
					{
						yield return startPoint;
						yield return next;
					}
					else
						yield return null;

					lastReturned = !lastReturned;
				}
			}
		}
	}

	/// <param name="Size">STARS default 0.5nmi.</param>
	public sealed record Arrow(PossiblyResolvedWaypoint[] Points, decimal Size) : GeoConnector(Points)
	{
		public override IEnumerable<Coordinate?> Draw()
		{
			if (_resolvedPoints.Length < 2)
				yield break;

			foreach (var (from, to) in _resolvedPoints[..^1].Zip(_resolvedPoints[1..]))
			{
				if (to.GetBearingDistance(from).bearing is not Course direction) continue;

				yield return from;
				yield return to;
				yield return to.FixRadialDistance(direction + 30m, Size);
				yield return to;
				yield return to.FixRadialDistance(direction - 30m, Size);
				yield return to;
			}
		}
	}
}