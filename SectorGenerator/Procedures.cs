using CIFPReader;

namespace SectorGenerator;
internal class Procedures(CIFP cifp)
{
	private readonly CIFP _cifp = cifp;

	public (string[] sidLines, NamedCoordinate[] fixes) AirportSidLines(string apIcao)
	{
		List<string> sidLines = [];
		HashSet<NamedCoordinate> fixes = [];
		_cifp.Runways.TryGetValue(apIcao, out var rws);
		if (!_cifp.Aerodromes.TryGetValue(apIcao, out Aerodrome? aerodrome))
			return ([], []);

		foreach (SID sid in _cifp.Procedures.Values.SelectMany(ps => ps.Where(p => p is SID s && s.Airport == apIcao)).Cast<SID>().OrderBy(s => s.Name))
		{
			bool globalHandled = false;
			HashSet<string> foundRunways = [];

			foreach (var (inboundTransition, outboundTransition) in sid.EnumerateTransitions())
			{
				string runways = inboundTransition?.Replace("RW", "") ?? (rws is null ? "" : string.Join(':', rws.Select(rw => rw.Identifier)));

				if (runways.EndsWith('B'))
					runways = $"{runways[..^1]}L:{runways[..^1]}R";

				foundRunways.UnionWith(runways.Split(':'));
				NamedCoordinate[] namedPointsOnProc = [..sid.SelectRoute(inboundTransition, outboundTransition)
					.Where(s => s.Endpoint is NamedCoordinate nc).Select(s => (NamedCoordinate)s.Endpoint!)];
				NamedCoordinate midPoint = namedPointsOnProc.Length > 0 ? namedPointsOnProc[namedPointsOnProc.Length / 2] : new(apIcao, new());

				if (outboundTransition is string ot)
					sidLines.Add($"{apIcao};{runways};{sid.Name}.{ot};{midPoint.Name};{midPoint.Name};1;");
				else
				{
					sidLines.Add($"{apIcao};{runways};{sid.Name};{midPoint.Name};{midPoint.Name};");
					globalHandled = true;
				}

				HashSet<string> sharedPoints = [];

				foreach (string runway in runways.Split(':'))
				{
					Coordinate startPoint;

					if (rws?.FirstOrDefault(r => r.Identifier == runway) is Runway startRw)
					{
						startPoint =
							_cifp.Runways[apIcao].FirstOrDefault(r => r.Identifier == startRw.OppositeIdentifier)?.Endpoint.GetCoordinate()
							?? startRw.Endpoint.GetCoordinate();
						string rwEnd = $"{apIcao}/RW{(startRw.OppositeIdentifier.TakeWhile(char.IsDigit).Count() < 2 ? "0" : "")}{startRw.OppositeIdentifier}";
						sidLines.Add($"{rwEnd};{rwEnd};<br>;");
					}
					else
						startPoint = aerodrome.Location.GetCoordinate();

					var (transitionLines, transitionFixes) = Run(startPoint, aerodrome.Elevation.Feet, aerodrome.MagneticVariation, apIcao, sid.SelectRoute(inboundTransition, outboundTransition));
					bool killNext = false;

					sidLines.AddRange(transitionLines.TakeWhile(l =>
					{
						if (killNext)
							return false;

						string[] segments = l.Split(';');
						killNext = segments.Length >= 2 && segments[0] == segments[1] && !sharedPoints.Add(segments[0]);
						return true;
					}));
					fixes.UnionWith(transitionFixes);
				}
			}

			if (!globalHandled && rws is not null)
			{
				string affectedRunways =
					foundRunways.Count > 0
					? string.Join(':', foundRunways)
					: rws is null
					  ? ""
					  : string.Join(':', rws.Select(rw => rw.Identifier));

				sidLines.Add($"{apIcao};{affectedRunways};{sid.Name};{apIcao};{apIcao};");
				var (massLines, massFixes) = Run(aerodrome.Location.GetCoordinate(), aerodrome.Elevation.Feet, aerodrome.MagneticVariation, apIcao, sid.SelectAllRoutes(_cifp.Fixes), cut: true);
				sidLines.AddRange(massLines);
				fixes.UnionWith(massFixes);
			}
		}

		return ([.. sidLines], [.. fixes]);
	}

	public (string[] starLines, NamedCoordinate[] fixes) AirportStarLines(string apIcao)
	{
		List<string> starLines = [];
		HashSet<NamedCoordinate> fixes = [];
		_cifp.Runways.TryGetValue(apIcao, out var rws);
		if (!_cifp.Aerodromes.TryGetValue(apIcao, out Aerodrome? aerodrome))
			return ([], []);

		foreach (STAR star in _cifp.Procedures.Values.SelectMany(ps => ps.Where(p => p is STAR s && s.Airport == apIcao)).Cast<STAR>().OrderBy(s => s.Name))
		{
			bool globalHandled = false;
			HashSet<string> foundRunways = [];

			foreach (var (inboundTransition, outboundTransition) in star.EnumerateTransitions())
			{
				string runways = outboundTransition?.Replace("RW", "") ?? (rws is null ? "" : string.Join(':', rws.Select(rw => rw.Identifier)));

				if (runways.EndsWith('B'))
					runways = $"{runways[..^1]}L:{runways[..^1]}R";

				foundRunways.UnionWith(runways.Split(':'));
				NamedCoordinate[] namedPointsOnProc = [..star.SelectRoute(inboundTransition, outboundTransition)
					.Where(s => s.Endpoint is NamedCoordinate nc).Select(s => (NamedCoordinate)s.Endpoint!)];
				NamedCoordinate midPoint = namedPointsOnProc.Length > 0 ? namedPointsOnProc[namedPointsOnProc.Length / 2] : new(apIcao, new());

				if (inboundTransition is string it)
					starLines.Add($"{apIcao};{runways};{it}.{star.Name};{midPoint.Name};{midPoint.Name};1;");
				else
				{
					starLines.Add($"{apIcao};{runways};{star.Name};{midPoint.Name};{midPoint.Name};");
					globalHandled = true;
				}

				Coordinate startPoint = _cifp.Aerodromes[apIcao].Location.GetCoordinate();

				var (transitionLines, transitionFixes) = Run(startPoint, aerodrome.Elevation.Feet, aerodrome.MagneticVariation, apIcao, star.SelectRoute(inboundTransition, outboundTransition));
				starLines.AddRange(transitionLines);
				fixes.UnionWith(transitionFixes);
			}

			if (!globalHandled)
			{
				string affectedRunways =
					foundRunways.Count > 0
					? string.Join(':', foundRunways)
					: rws is null
					  ? ""
					  : string.Join(':', rws.Select(rw => rw.Identifier));

				starLines.Add($"{apIcao};{affectedRunways};{star.Name};{apIcao};{apIcao};");
				var (massLines, massFixes) = Run(aerodrome.Location.GetCoordinate(), aerodrome.Elevation.Feet, aerodrome.MagneticVariation, apIcao, star.SelectAllRoutes(_cifp.Fixes));
				starLines.AddRange(massLines);
				fixes.UnionWith(massFixes);
			}
		}

		return ([.. starLines], [.. fixes]);
	}

	public (string[] iapLines, NamedCoordinate[] fixes) AirportApproachLines(string apIcao)
	{
		List<string> iapLines = [];
		HashSet<NamedCoordinate> fixes = [];
		_cifp.Runways.TryGetValue(apIcao, out var rws);
		if (!_cifp.Aerodromes.TryGetValue(apIcao, out Aerodrome? aerodrome))
			return ([], []);

		if (rws is null)
			return ([], []);

		string allRunways = string.Join(':', rws.Select(rw => rw.Identifier));

		foreach (Approach iap in _cifp.Procedures.Values.SelectMany(ps => ps.Where(p => p is Approach a && a.Airport == apIcao)).Cast<Approach>().OrderBy(s => s.Name))
		{
			if (iap.Name.Length < 3)
			{
				Console.Error.WriteLine($"Weirdly short procedure {iap.Name} at {iap.Airport}. Skipped.");
				continue;
			}

			string iapName =
				iap.Name[..3] switch {
					"VOR" or "NDB" or "GPS" => iap.Name,
					"VDM" => "DME" + iap.Name[3..],
					_ => iap.Name[0] switch {
						'I' => "ILS",
						'L' => "LOC",
						'B' => "BC",
						'R' or 'H' => "RNV",
						'S' or 'V' => "VOR",
						'Q' or 'N' => "NDB",
						'D' => "DME",
						'X' => "LDA",
						'P' => "GPS",
						'U' => "SDF",
						_ => ((Func<string>)(() => { Console.WriteLine(iap.Airport + ": " + iap.Name); return "???"; }))()
					} + iap.Name[1..]
				};

			string runways = new([.. iapName.SkipWhile(char.IsLetter).TakeWhile(c => char.IsDigit(c) || "LCRBA".Contains(c))]);

			if (runways.EndsWith('B') && runways.Any(char.IsDigit))
				runways = $"{runways[..^1]}L:{runways[..^1]}R";

			ICoordinate midPoint = (ICoordinate)(iap.SelectRoute(null, null).FirstOrDefault(i => i.Endpoint is ICoordinate)?.Endpoint ?? aerodrome.Location);

			if (midPoint is NamedCoordinate nc)
				iapLines.Add($"{apIcao};{runways};{iapName};{nc.Name};{nc.Name};3;");
			else
				iapLines.Add($"{apIcao};{runways};{iapName};{midPoint.Latitude:00.0####};{midPoint.Longitude:000.0####};3;");

			var (massLines, massFixes) = Run(aerodrome.Location.GetCoordinate(), aerodrome.Elevation.Feet, aerodrome.MagneticVariation, apIcao, iap.SelectAllRoutes(_cifp.Fixes));
			iapLines.AddRange(massLines);
			fixes.UnionWith(massFixes);
		}

		return ([.. iapLines], [.. fixes]);
	}

	private static string lastLine = "";
	private static (string[] Lines, NamedCoordinate[] Fixes) Run(Coordinate startPoint, int elevation, decimal magVar, string airportIcao, IEnumerable<Procedure.Instruction?> instructions, bool cut = false)
	{
		List<string> lines = [];
		HashSet<NamedCoordinate> fixes = [];
		Procedure.Instruction? state = null;
		bool breakPending = false;

		void addLine(string line)
		{
			if (line == lastLine)
				return;

			lines.Add(line);
			lastLine = line;
		}

		bool isCut = false;
		foreach (var instruction in instructions)
		{
			if (isCut && instruction is not null)
				continue;

			if (instruction is null)
			{
				isCut = false;
				breakPending = true;
				state = null;
				continue;
			}

			if (cut && instruction.Endpoint is NamedCoordinate iepnc && fixes.Contains(iepnc))
				isCut = true;

			var (newCoords, newState) = Step(startPoint, elevation, magVar, airportIcao, instruction, state);
			state = newState;

			if (newCoords.Length == 0)
				continue;

			foreach ((ICoordinate epc, AltitudeRestriction ar) in newCoords)
			{
				if (epc is NamedCoordinate nc)
				{
					fixes.Add(nc);
					addLine($"{nc.Name};{nc.Name};{(ar == AltitudeRestriction.Unrestricted ? "" : $"{ar};")}");
				}
				else if (epc is Coordinate c)
					addLine($"{c.Latitude:00.0####};{c.Longitude:000.0####};{(ar == AltitudeRestriction.Unrestricted ? "" : $"{ar};")}");
				else throw new NotImplementedException();

				if (breakPending && lines.Count > 0)
				{
					breakPending = false;

					if (lines[^1].Count(c => c == ';') <= 2)
						lines[^1] += "<br>;";
					else
						addLine(string.Join(';', lines[^1].Split(';')[..2]) + ";<br>;");

					lastLine = lines[^1];
				}
			}

			startPoint = newCoords[^1].Endpoint.GetCoordinate();
			breakPending |= instruction.Termination.HasFlag(ProcedureLine.PathTermination.UntilTerminated);
		}

		return ([.. lines], [.. fixes]);
	}

	private static ((ICoordinate Endpoint, AltitudeRestriction Altitude)[] Points, Procedure.Instruction? State) Step(ICoordinate startingPoint, int airportElevation, decimal magVar, string airportIcao, Procedure.Instruction instruction, Procedure.Instruction? state = null)
	{
		IEnumerable<(ICoordinate, AltitudeRestriction)> procPrev()
		{
			const double DEG_TO_RAD = Math.PI / 180;

			if (state is null || state.Via is not Course viaCourse || instruction.ReferencePoint is null || instruction.Via is not Course interceptCourse)
				yield break;

			var (refLat, refLon) = ((double)instruction.ReferencePoint.Latitude, (double)instruction.ReferencePoint.Longitude);
			var (lastLat, lastLon) = ((double)startingPoint.Latitude, (double)startingPoint.Longitude);
			var (sinAngle, cosAngle) = Math.SinCos((double)interceptCourse.Radians);

			double distToIntercept = Math.Abs(cosAngle * (refLat - lastLat) - sinAngle * (refLon - lastLon) * Math.Cos(DEG_TO_RAD * (double)startingPoint.Latitude));
			yield return (startingPoint.GetCoordinate().FixRadialDistance(viaCourse, (decimal)distToIntercept), state.Altitude);
		}

		Distance? distance =
			instruction.Termination.HasFlag(ProcedureLine.PathTermination.ForDistance)
			? instruction.Endpoint as Distance
			: instruction.Termination.HasFlag(ProcedureLine.PathTermination.UntilAltitude) && instruction.Altitude.Minimum is Altitude endAlt
			  ? new(startingPoint, endAlt.ToAGL(airportElevation).Feet / 200) // 200ft per nmi climb standard gradient.
			  : null;

		if (instruction.Termination.HasFlag(ProcedureLine.PathTermination.Hold) && instruction.Via is Racetrack hold && hold.Point is not null)
		{
			const decimal radius = 1; // NMI

			List<Coordinate> rtPoints = [];
			Course inboundCourse = hold.InboundCourse is MagneticCourse imc ? imc.Resolve(magVar) : hold.InboundCourse;
			Course outboundCourse = inboundCourse.Reciprocal;
			Coordinate focus1 = hold.Point.GetCoordinate().FixRadialDistance(inboundCourse + (hold.LeftTurns ? -90m : 90m), radius),
					   focus2 = focus1.FixRadialDistance(outboundCourse, radius * 3.5m);

			for (decimal angle = -90m; angle <= 90m; angle += 15)
				rtPoints.Add(focus1.FixRadialDistance(inboundCourse + angle, radius));

			for (decimal angle = 90m; angle <= 270m; angle += 15)
				rtPoints.Add(focus2.FixRadialDistance(inboundCourse + angle, radius));

			return ([.. procPrev(), (hold.Point, instruction.Altitude), .. rtPoints.Select(p => (p, AltitudeRestriction.Unrestricted)), (hold.Point, AltitudeRestriction.Unrestricted)], null);
		}
		else if (instruction.Termination.HasFlag(ProcedureLine.PathTermination.UntilCrossing))
		{
			if (instruction.Endpoint is NamedCoordinate nep && nep.Name.StartsWith("RW"))
				return ([.. procPrev(), (nep with { Name = $"{airportIcao}/{nep.Name}" }, instruction.Altitude)], null);
			if (instruction.Endpoint is ICoordinate dep)
				return ([.. procPrev(), (dep, instruction.Altitude)], null);
			else
				throw new NotImplementedException();
		}
		else if (distance is not null)
		{
			if (instruction.Termination.HasFlag(ProcedureLine.PathTermination.Heading) ||
				 instruction.Termination.HasFlag(ProcedureLine.PathTermination.Track) ||
				 instruction.Termination.HasFlag(ProcedureLine.PathTermination.Course))
			{
				if (instruction.Via is not Course c)
					throw new NotImplementedException();

				return ([.. procPrev(), (startingPoint.GetCoordinate().FixRadialDistance(c, distance.NMI), instruction.Altitude)], null);
			}
			else if (instruction.Termination.HasFlag(ProcedureLine.PathTermination.Direct))
				return ([.. procPrev(), (startingPoint.GetCoordinate().FixRadialDistance(
					startingPoint.GetCoordinate().GetBearingDistance(((ICoordinate)instruction.Endpoint!).GetCoordinate()).bearing ?? new(0),
					distance.NMI
				), instruction.Altitude)], null);
			else
				throw new NotImplementedException();
		}
		else if (instruction.Termination.HasFlag(ProcedureLine.PathTermination.UntilTerminated))
		{
			if ((instruction.Termination.HasFlag(ProcedureLine.PathTermination.Heading) ||
				 instruction.Termination.HasFlag(ProcedureLine.PathTermination.Track) ||
				 instruction.Termination.HasFlag(ProcedureLine.PathTermination.Course)) && instruction.Via is Course hdg)
				return ([.. procPrev(), (startingPoint.GetCoordinate().FixRadialDistance(hdg, 0.25m), instruction.Altitude)], null);
			else
				throw new NotImplementedException();
		}
		else if (instruction.Termination.HasFlag(ProcedureLine.PathTermination.Arc) && instruction.Via is Arc arc && arc.Centerpoint is not null && instruction.Endpoint is ICoordinate arcEnd)
		{
			Coordinate arcCenter = arc.Centerpoint.GetCoordinate();
			TrueCourse startAngle = arcCenter.GetBearingDistance(startingPoint.GetCoordinate()).bearing ?? new(0);
			TrueCourse endAngle = arcCenter.GetBearingDistance(arcEnd.GetCoordinate()).bearing ?? new(0);

			decimal totalAngle = startAngle.Angle(endAngle);
			bool up = arcEnd.Latitude < arcCenter.Latitude ^ arc.ArcTo.Degrees < 180;

			List<Coordinate> intermediatePoints = [];
			for (Course angle = startAngle; up ? angle.Degrees < endAngle.Degrees : angle.Degrees > endAngle.Degrees; angle += up ? 15m : -15m)
				intermediatePoints.Add(arcCenter.FixRadialDistance(angle, arc.Radius));

			return ([.. procPrev(), .. intermediatePoints.Select(p => (p, AltitudeRestriction.Unrestricted))], null);
		}
		else
			return ([.. procPrev()], instruction);
	}
}
