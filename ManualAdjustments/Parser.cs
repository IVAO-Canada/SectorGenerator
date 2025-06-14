using System.Collections.Immutable;

using static ManualAdjustments.Twe.Token;

namespace ManualAdjustments;

internal class Parser(string input)
{
	readonly ImmutableDictionary<int, ImmutableHashSet<Twe>> _tweSet = Parsing.Lex(input);
	readonly HashSet<AddFix> _adHocFixes = [];

	private (ImmutableHashSet<Twe> Twes, int Position) Twes(int position) =>
		_tweSet.TryGetValue(position, out var retval)
		? (retval, position)
		: (position > 0 && _tweSet.Keys.FirstOrDefault(k => k > position) is int key && key is not 0)
		  ? (_tweSet[key], key)
		  : ([], -1);

	public ParseResult ParseFile(int position, ImmutableStack<Twe.Token[]> synchroSets)
	{
		var childSynchro = synchroSets.Push([DefType]);
		List<ParseResult> children = [];
		List<ManualAdjustment> adjustments = [];

		Range range = new(new(0, 0), new(0, 0));

		while (((_, position) = Twes(position)).Item1.FirstOrDefault(static twe => twe.Type is DefType) is Twe defType)
		{
			var result = ParseAdjustment(position, childSynchro);
			children.Add(result);

			Type resultType = result.GetType();

			if (resultType.IsGenericType)
				adjustments.Add(((dynamic)result).Result);

			position = result.NextIdx;
			range = range.ExpandTo(result.Range);
		}

		if (position == -1 || position >= _tweSet.Keys.Max())
			return new ParseResult<ManualAdjustment[]>(
				position,
				range,
				[.. _adHocFixes, .. adjustments.Where(static a => a is AddFix or AddVfrFix), .. adjustments.Where(static a => a is not (AddFix or AddVfrFix))],
				[],
				[.. children]
			);
		else
			return new ParseResult.Failed(position, range, [], [], [.. children]);
	}

	public ParseResult ParseAdjustment(int position, ImmutableStack<Twe.Token[]> synchroSets)
	{
		List<Twe> literals = [];
		(var twes, position) = Twes(position);
		var childSynchro = synchroSets.Push([]);
		void advanceBy(int delta) => (twes, position) = Twes(position + delta);
		void advanceTo(int pos) => (twes, position) = Twes(pos);

		if (twes.FirstOrDefault(static twe => twe.Type is DefType) is not Twe discriminator)
		{
			(bool shouldReturn, int newPos) = Synchro(position, synchroSets, DefType);
			string skippedSegment = position is -1 || newPos is -1 ? "" : input[position..newPos];
			Range priorRange = twes.IsEmpty ? new(new(0, 0), new(0, 0)) : new(twes.First().Position.Start, twes.First().Position.Start);
			advanceTo(newPos);

			if (shouldReturn)
				return new ParseResult.Failed(
					position,
					priorRange,
					[new Edit.Deletion(new(priorRange.End, twes.FirstOrDefault()?.Position.Start ?? priorRange.End))],
					[],
					[]
				);
			else if (twes.FirstOrDefault(static twe => twe.Type is DefType) is Twe newDiscrim)
				discriminator = newDiscrim;
			else
				throw new Exception("Synchro failed!");
		}

		literals.Add(discriminator);
		advanceBy(discriminator.Lexeme.Length);

		if (discriminator.Lexeme is "FIX" or "VFRFIX")
		{
			// Add or delete a fix.
			if (twes.FirstOrDefault(static twe => twe.Type is Name) is not Twe fixName)
			{
				(bool shouldReturn, int newPos) = Synchro(position, synchroSets, Name);
				string skippedSegment = position is -1 || newPos is -1 ? "" : input[position..newPos];
				advanceTo(newPos);

				if (shouldReturn)
					return new ParseResult.Failed(
						position,
						discriminator.Position,
						[new Edit.Deletion(new(discriminator.Position.End, twes.FirstOrDefault()?.Position.Start ?? discriminator.Position.End))],
						[..literals],
						[]
					);
				else if (twes.FirstOrDefault(static twe => twe.Type is Name) is Twe newFix)
					fixName = newFix;
				else
					throw new Exception("Synchro failed!");
			}

			literals.Add(fixName);
			advanceBy(fixName.Lexeme.Length);
			if (twes.FirstOrDefault(static twe => twe.Type is Colon) is not Twe colon)
			{
				(bool shouldReturn, int newPos) = Synchro(position, synchroSets, Colon);
				string skippedSegment = position is -1 || newPos is -1 ? "" : input[position..newPos];
				Range priorRange = discriminator.Position.ExpandTo(fixName.Position);
				advanceTo(newPos);

				if (shouldReturn)
					return new ParseResult.Failed(
						position,
						priorRange,
						[new Edit.Deletion(new(priorRange.End, twes.FirstOrDefault()?.Position.Start ?? priorRange.End))],
						[..literals],
						[]
					);
				else if (twes.FirstOrDefault(static twe => twe.Type is Colon) is Twe newColon)
					colon = newColon;
				else
					throw new Exception("Synchro failed!");
			}

			literals.Add(colon);
			advanceBy(colon.Lexeme.Length);
			if (twes.FirstOrDefault(static twe => twe.Type is Delete) is Twe delete)
			{
				// Delete a fix.
				advanceBy(delete.Lexeme.Length);

				return new ParseResult<RemoveFix>(
					position,
					discriminator.Position.ExpandTo(fixName.Position).ExpandTo(colon.Position).ExpandTo(delete.Position),
					new RemoveFix(new(null, new(fixName.Lexeme.Trim('"')), null)),
					[..literals, delete],
					[]
				);
			}

			// Add a fix.
			ParseResult location = ParseLocation(position, childSynchro);

			if (location is ParseResult<PossiblyResolvedWaypoint> waypoint)
				return discriminator.Lexeme is "FIX"
				? new ParseResult<AddFix>(
					waypoint.NextIdx,
					discriminator.Position.ExpandTo(fixName.Position).ExpandTo(colon.Position).ExpandTo(waypoint.Range),
					new(fixName.Lexeme.Trim('"'), waypoint.Result),
					[..literals],
					[waypoint]
				)
				: new ParseResult<AddVfrFix>(
					waypoint.NextIdx,
					discriminator.Position.ExpandTo(fixName.Position).ExpandTo(colon.Position).ExpandTo(waypoint.Range),
					new(fixName.Lexeme.Trim('"'), waypoint.Result),
					[..literals],
					[waypoint]
				);
			else
				return new ParseResult.Failed(
					location.NextIdx,
					discriminator.Position.ExpandTo(fixName.Position).ExpandTo(colon.Position).ExpandTo(location.Range),
					[],
					[..literals],
					[location]
				);
		}
		else if (discriminator.Lexeme is "AIRWAY")
		{
			// Define an airway.
			if (twes.FirstOrDefault(static twe => twe.Type is AirwayType) is not Twe airwayTypeStr)
			{
				(bool shouldReturn, int newPos) = Synchro(position, synchroSets, AirwayType);
				string skippedSegment = position is -1 || newPos is -1 ? "" : input[position..newPos];
				advanceTo(newPos);

				if (shouldReturn)
					return new ParseResult.Failed(
						position,
						discriminator.Position,
						[new Edit.Deletion(new(discriminator.Position.End, twes.FirstOrDefault()?.Position.Start ?? discriminator.Position.End))],
						[..literals],
						[]
					);
				else if (twes.FirstOrDefault(static twe => twe.Type is AirwayType) is Twe newAwType)
					airwayTypeStr = newAwType;
				else
					throw new Exception("Synchro failed!");
			}

			literals.Add(airwayTypeStr);
			advanceBy(airwayTypeStr.Lexeme.Length);
			AddAirway.AirwayType airwayType = airwayTypeStr.Lexeme switch {
				"HIGH" => AddAirway.AirwayType.High,
				"LOW" => AddAirway.AirwayType.Low,
				_ => throw new NotImplementedException()
			};

			if (twes.FirstOrDefault(static twe => twe.Type is Name) is not Twe routeName)
			{
				(bool shouldReturn, int newPos) = Synchro(position, synchroSets, Name);
				string skippedSegment = position is -1 || newPos is -1 ? "" : input[position..newPos];
				Range priorRange = discriminator.Position.ExpandTo(airwayTypeStr.Position);
				advanceTo(newPos);

				if (shouldReturn)
					return new ParseResult.Failed(
						position,
						priorRange,
						[new Edit.Deletion(new(priorRange.End, twes.FirstOrDefault()?.Position.Start ?? priorRange.End))],
						[..literals],
						[]
					);
				else if (twes.FirstOrDefault(static twe => twe.Type is Name) is Twe newRteName)
					routeName = newRteName;
				else
					throw new Exception("Synchro failed!");
			}

			literals.Add(routeName);
			advanceBy(routeName.Lexeme.Length);
			if (twes.FirstOrDefault(static twe => twe.Type is Colon) is not Twe colon)
			{
				(bool shouldReturn, int newPos) = Synchro(position, synchroSets, Colon);
				string skippedSegment = position is -1 || newPos is -1 ? "" : input[position..newPos];
				Range priorRange = discriminator.Position.ExpandTo(airwayTypeStr.Position).ExpandTo(routeName.Position);
				advanceTo(newPos);

				if (shouldReturn)
					return new ParseResult.Failed(
						position,
						priorRange,
						[new Edit.Deletion(new(priorRange.End, twes.FirstOrDefault()?.Position.Start ?? priorRange.End))],
						[..literals],
						[]
					);
				else if (twes.FirstOrDefault(static twe => twe.Type is Colon) is Twe newColon)
					colon = newColon;
				else
					throw new Exception("Synchro failed!");
			}

			literals.Add(colon);
			advanceBy(colon.Lexeme.Length);
			ParseResult locationResult = ParseLocationList(position, childSynchro);

			PossiblyResolvedWaypoint[] waypoints =
				locationResult is ParseResult<PossiblyResolvedWaypoint[]> s
				? s.Result
				: [];

			return new ParseResult<AddAirway>(
				locationResult.NextIdx,
				discriminator.Position.ExpandTo(airwayTypeStr.Position).ExpandTo(routeName.Position).ExpandTo(colon.Position).ExpandTo(locationResult.Range),
				new(waypoints, airwayType, routeName.Lexeme.Trim('"')),
				[.. literals],
				[locationResult]
			);
		}
		else if (discriminator.Lexeme is "VFRROUTE")
		{
			// Define a VFR route.
			if (twes.FirstOrDefault(static twe => twe.Type is Name) is not Twe routeName)
			{
				(bool shouldReturn, int newPos) = Synchro(position, synchroSets, Name);
				string skippedSegment = position is -1 || newPos is -1 ? "" : input[position..newPos];
				advanceTo(newPos);

				if (shouldReturn)
					return new ParseResult.Failed(
						position,
						discriminator.Position,
						[new Edit.Deletion(new(discriminator.Position.End, twes.FirstOrDefault()?.Position.Start ?? discriminator.Position.End))],
						[..literals],
						[]
					);
				else if (twes.FirstOrDefault(static twe => twe.Type is Name) is Twe newRteName)
					routeName = newRteName;
				else
					throw new Exception("Synchro failed!");
			}

			literals.Add(routeName);
			advanceBy(routeName.Lexeme.Length);
			if (twes.FirstOrDefault(static twe => twe.Type is Colon) is not Twe colon)
			{
				(bool shouldReturn, int newPos) = Synchro(position, synchroSets, Colon);
				string skippedSegment = position is -1 || newPos is -1 ? "" : input[position..newPos];
				Range priorRange = discriminator.Position.ExpandTo(routeName.Position);
				advanceTo(newPos);

				if (shouldReturn)
					return new ParseResult.Failed(
						position,
						priorRange,
						[new Edit.Deletion(new(priorRange.End, twes.FirstOrDefault()?.Position.Start ?? priorRange.End))],
						[..literals],
						[]
					);
				else if (twes.FirstOrDefault(static twe => twe.Type is Colon) is Twe newColon)
					colon = newColon;
				else
					throw new Exception("Synchro failed!");
			}

			literals.Add(colon);
			advanceBy(colon.Lexeme.Length);
			ParseResult locationResult = ParseLocationList(position, childSynchro);

			PossiblyResolvedWaypoint[] waypoints =
				locationResult is ParseResult<PossiblyResolvedWaypoint[]> s
				? s.Result
				: [];

			return new ParseResult<AddVfrRoute>(
				locationResult.NextIdx,
				discriminator.Position.ExpandTo(routeName.Position).ExpandTo(colon.Position).ExpandTo(locationResult.Range),
				new(routeName.Lexeme.Trim('"'), waypoints),
				[..literals],
				[locationResult]
			);
		}
		else if (discriminator.Lexeme is "GEO")
		{
			// Define a GEO/video map.
			if (twes.FirstOrDefault(static twe => twe.Type is Name) is not Twe geoName)
			{
				(bool shouldReturn, int newPos) = Synchro(position, synchroSets, Name);
				string skippedSegment = position is -1 || newPos is -1 ? "" : input[position..newPos];
				advanceTo(newPos);

				if (shouldReturn)
					return new ParseResult.Failed(
						position,
						discriminator.Position,
						[new Edit.Deletion(new(discriminator.Position.End, twes.FirstOrDefault()?.Position.Start ?? discriminator.Position.End))],
						[..literals],
						[]
					);
				else if (twes.FirstOrDefault(static twe => twe.Type is Name) is Twe newGeoName)
					geoName = newGeoName;
				else
					throw new Exception("Synchro failed!");
			}

			literals.Add(geoName);
			advanceBy(geoName.Lexeme.Length);
			string colour = "#FF999999";
			if (twes.FirstOrDefault(static twe => twe.Type is Parameter) is Twe param)
			{
				advanceBy(param.Lexeme.Length);
				colour = param.Lexeme[1..^1];
				literals.Add(param);
			}

			if (twes.FirstOrDefault(static twe => twe.Type is Colon) is not Twe colon)
			{
				(bool shouldReturn, int newPos) = Synchro(position, synchroSets, Colon);
				string skippedSegment = position is -1 || newPos is -1 ? "" : input[position..newPos];
				Range priorRange = discriminator.Position.ExpandTo(geoName.Position);
				advanceTo(newPos);

				if (shouldReturn)
					return new ParseResult.Failed(
						position,
						priorRange,
						[new Edit.Deletion(new(priorRange.End, twes.FirstOrDefault()?.Position.Start ?? priorRange.End))],
						[..literals],
						[]
					);
				else if (twes.FirstOrDefault(static twe => twe.Type is Colon) is Twe newColon)
					colon = newColon;
				else
					throw new Exception("Synchro failed!");
			}

			literals.Add(colon);
			advanceBy(colon.Lexeme.Length);
			ParseResult geos = ParseGeoList(position, childSynchro);

			return new ParseResult<AddGeo>(
				geos.NextIdx,
				discriminator.Position.ExpandTo(geoName.Position).ExpandTo(colon.Position).ExpandTo(geos.Range),
				new(geoName.Lexeme.Trim('"'), colour, geos is ParseResult<IDrawableGeo[]> gs ? gs.Result : []),
				[..literals],
				[geos]
			);
		}
		else if (discriminator.Lexeme is "PROC")
		{
			// Define a procedure.
			if (twes.FirstOrDefault(static twe => twe.Type is Airport) is not Twe airport)
			{
				(bool shouldReturn, int newPos) = Synchro(position, synchroSets, Airport);
				string skippedSegment = position is -1 || newPos is -1 ? "" : input[position..newPos];
				advanceTo(newPos);

				if (shouldReturn)
					return new ParseResult.Failed(
						position,
						discriminator.Position,
						[new Edit.Deletion(new(discriminator.Position.End, twes.FirstOrDefault()?.Position.Start ?? discriminator.Position.End))],
						[..literals],
						[]
					);
				else if (twes.FirstOrDefault(static twe => twe.Type is Airport) is Twe newAirport)
					airport = newAirport;
				else
					throw new Exception("Synchro failed!");
			}

			literals.Add(airport);
			advanceBy(airport.Lexeme.Length);
			if (twes.FirstOrDefault(static twe => twe.Type is ProcType) is not Twe procTypeStr)
			{
				(bool shouldReturn, int newPos) = Synchro(position, synchroSets, ProcType);
				string skippedSegment = position is -1 || newPos is -1 ? "" : input[position..newPos];
				Range priorRange = discriminator.Position.ExpandTo(airport.Position);
				advanceTo(newPos);

				if (shouldReturn)
					return new ParseResult.Failed(
						position,
						priorRange,
						[new Edit.Deletion(new(priorRange.End, twes.FirstOrDefault()?.Position.Start ?? priorRange.End))],
						[..literals],
						[]
					);
				else if (twes.FirstOrDefault(static twe => twe.Type is ProcType) is Twe newProcTypeStr)
					procTypeStr = newProcTypeStr;
				else
					throw new Exception("Synchro failed!");
			}

			literals.Add(procTypeStr);
			advanceBy(procTypeStr.Lexeme.Length);
			AddProcedure.ProcedureType procType = procTypeStr.Lexeme switch {
				"SID" => AddProcedure.ProcedureType.SID,
				"STAR" => AddProcedure.ProcedureType.STAR,
				"IAP" => AddProcedure.ProcedureType.IAP,
				_ => throw new System.Diagnostics.UnreachableException()
			};

			if (twes.FirstOrDefault(static twe => twe.Type is Name) is not Twe procName)
			{
				(bool shouldReturn, int newPos) = Synchro(position, synchroSets, Name);
				string skippedSegment = position is -1 || newPos is -1 ? "" : input[position..newPos];
				Range priorRange = discriminator.Position.ExpandTo(airport.Position);
				advanceTo(newPos);

				if (shouldReturn)
					return new ParseResult.Failed(
						position,
						priorRange,
						[new Edit.Deletion(new(priorRange.End, twes.FirstOrDefault()?.Position.Start ?? priorRange.End))],
						[..literals],
						[]
					);
				else if (twes.FirstOrDefault(static twe => twe.Type is Name) is Twe newProcName)
					procName = newProcName;
				else
					throw new Exception("Synchro failed!");
			}

			literals.Add(procName);
			advanceBy(procName.Lexeme.Length);
			if (twes.FirstOrDefault(static twe => twe.Type is Colon) is not Twe colon)
			{
				(bool shouldReturn, int newPos) = Synchro(position, synchroSets, Colon);
				string skippedSegment = position is -1 || newPos is -1 ? "" : input[position..newPos];
				Range priorRange = discriminator.Position.ExpandTo(airport.Position);
				advanceTo(newPos);

				if (shouldReturn)
					return new ParseResult.Failed(
						position,
						priorRange,
						[new Edit.Deletion(new(priorRange.End, twes.FirstOrDefault()?.Position.Start ?? priorRange.End))],
						[..literals],
						[]
					);
				else if (twes.FirstOrDefault(static twe => twe.Type is Colon) is Twe newColon)
					colon = newColon;
				else
					throw new Exception("Synchro failed!");
			}

			literals.Add(colon);
			advanceBy(colon.Lexeme.Length);
			Range intermediateProcRange = discriminator.Position.ExpandTo(airport.Position).ExpandTo(procTypeStr.Position).ExpandTo(colon.Position);
			if (twes.FirstOrDefault(static twe => twe.Type is Delete) is Twe deletion)
			{
				advanceBy(deletion.Lexeme.Length);
				return new ParseResult<RemoveProcedure>(
					position,
					intermediateProcRange.ExpandTo(deletion.Position),
					new(airport.Lexeme, procType, procName.Lexeme),
					[..literals],
					[]
				);
			}

			ParseResult geos = ParseGeoList(position, childSynchro);

			return new ParseResult<AddProcedure>(
				geos.NextIdx,
				intermediateProcRange.ExpandTo(geos.Range),
				new(airport.Lexeme, procType, procName.Lexeme, geos is ParseResult<IDrawableGeo[]> gs ? gs.Result : []),
				[..literals],
				[geos]
			);
		}
		else
			throw new NotImplementedException();
	}

	public ParseResult ParseGeoList(int position, ImmutableStack<Twe.Token[]> synchroSets)
	{
		(var twe, position) = Twes(position);
		void advanceBy(int delta)
		{
			position += delta;
			(twe, position) = Twes(position);
		}

		Range range = new(new(int.MaxValue, int.MaxValue), new(int.MinValue, int.MinValue));
		List<ParseResult> children = [];

		// Parse the GEOs, one-by-one.
		while (twe.FirstOrDefault(static twe => twe.Type is Indent) is Twe indent)
		{
			List<Twe> literals = [];

			// Eat the indentation.
			range = range.ExpandTo(indent.Position);
			advanceBy(indent.Lexeme.Length);

			// Check if it's a symbol or a connector.
			if (twe.Where(static twe => twe.Type is ConnectorType).MaxBy(static twe => twe.Lexeme.Length) is Twe connectorType)
			{
				// Connector!
				literals.Add(connectorType);
				advanceBy(connectorType.Lexeme.Length);
				range = range.ExpandTo(connectorType.Position);

				string? param = null;

				if (twe.FirstOrDefault(static twe => twe.Type is Parameter) is Twe paramTwe)
				{
					// Optional parameter was included.
					literals.Add(paramTwe);
					advanceBy(paramTwe.Lexeme.Length);
					range = range.ExpandTo(paramTwe.Position);
					// Scrape off the ()s.
					param = paramTwe.Lexeme[1..^1];

					if (connectorType.Lexeme is "DASH" or "ARROW" && !decimal.TryParse(param, out _))
						// TODO: Report incorrect parameter type.
						throw new NotImplementedException();
				}

				// Get the locations of the connector's points.
				ParseResult possibleLocations = ParseLocationList(
					position,
					synchroSets.Push([Indent])
				);

				(twe, position) = Twes(possibleLocations.NextIdx);

				Range connectorRange = indent.Position
					.ExpandTo(connectorType.Position)
					.ExpandTo(possibleLocations.Range);

				PossiblyResolvedWaypoint[] locations = [];

				if (possibleLocations is ParseResult<PossiblyResolvedWaypoint[]> successChild)
					locations = successChild.Result;

				connectorRange = connectorRange.ExpandTo(possibleLocations.Range);
				range = range.ExpandTo(connectorRange);
				GeoConnector.Arc.Direction arcDirection = param switch {
					"S" => GeoConnector.Arc.Direction.South,
					"E" => GeoConnector.Arc.Direction.East,
					"W" => GeoConnector.Arc.Direction.West,
					_ => GeoConnector.Arc.Direction.North
				};

				// Construct the correct GEO for the definition.
				GeoConnector connector = connectorType.Lexeme switch {
					"LINE" => new GeoConnector.Line(locations),
					"DASH" => new GeoConnector.Dash(locations, param is null ? 1m : decimal.Parse(param)),
					"ARROW" => new GeoConnector.Arrow(locations, param is null ? 0.25m : decimal.Parse(param)),
					"ARC" => new GeoConnector.Arc(locations, arcDirection),
					"DASHARC" => new GeoConnector.DashArc(locations, arcDirection),
					_ => throw new NotImplementedException()
				};

				children.Add(new ParseResult<IDrawableGeo>(
					position,
					connectorRange,
					connector,
					[..literals],
					[possibleLocations]
				));
			}
			else if (twe.FirstOrDefault(static twe => twe.Type is SymbolType) is Twe symbolType)
			{
				// Symbol!
				literals.Add(symbolType);
				advanceBy(symbolType.Lexeme.Length);
				range = range.ExpandTo(symbolType.Position);

				decimal? param = null;

				if (twe.FirstOrDefault(static twe => twe.Type is Parameter) is Twe paramTwe)
				{
					// Optional parameter was included.
					literals.Add(paramTwe);
					advanceBy(paramTwe.Lexeme.Length);
					range = range.ExpandTo(paramTwe.Position);
					// Scrape off the ()s.
					if (decimal.TryParse(paramTwe.Lexeme[1..^1], out decimal paramDec))
						param = paramDec;
					else
						// TODO: Report incorrect parameter type.
						throw new NotImplementedException();
				}

				// Get the location of the symbol.
				ParseResult possibleLocation = ParseLocation(
					position,
					synchroSets.Push([Indent])
				);

				(twe, position) = Twes(possibleLocation.NextIdx);

				Range symbolRange = indent.Position
					.ExpandTo(symbolType.Position)
					.ExpandTo(possibleLocation.Range);

				if (possibleLocation is not ParseResult<PossiblyResolvedWaypoint> location)
				{
					// Can't even try to create the GEO without a centrepoint.
					children.Add(new ParseResult.Failed(
						possibleLocation.NextIdx,
						symbolRange,
						[],
						[..literals],
						[possibleLocation]
					));
					continue;
				}

				symbolRange = symbolRange.ExpandTo(possibleLocation.Range);
				range = range.ExpandTo(symbolRange);
				// Construct the correct GEO for the definition.
				GeoSymbol symbol = symbolType.Lexeme switch {
					"POINT" => new GeoSymbol.Point(location.Result),
					"CIRCLE" => new GeoSymbol.Circle(location.Result, param ?? 0.5m),
					"WAYPOINT" => new GeoSymbol.Waypoint(location.Result, param ?? 1m),
					"TRIANGLE" => new GeoSymbol.Triangle(location.Result, param ?? 0.5m),
					"NUCLEAR" => new GeoSymbol.Nuclear(location.Result, param ?? 1m),
					"FLAG" => new GeoSymbol.Flag(location.Result, param ?? 1m),
					"DIAMOND" => new GeoSymbol.Diamond(location.Result, param ?? 1m),
					"CHEVRON" => new GeoSymbol.Chevron(location.Result, param ?? 0.25m),
					"BOX" => new GeoSymbol.Box(location.Result, param ?? 0.5m),
					"STAR" => new GeoSymbol.Star(location.Result, param ?? 1m),
					_ => throw new NotImplementedException()
				};

				children.Add(new ParseResult<IDrawableGeo>(
					position,
					symbolRange,
					symbol,
					[..literals],
					[location]
				));
			}
			else
			{
				// No clue!
				(bool shouldReturn, int newPos) = Synchro(position, synchroSets, Indent);
				string skippedSegment = position is -1 || newPos is -1 ? "" : input[position..newPos];
				(twe, position) = Twes(newPos);

				if (shouldReturn)
					return new ParseResult.Failed(
						position,
						indent.Position,
						[new Edit.Deletion(new(indent.Position.End, twe.FirstOrDefault()?.Position.Start ?? indent.Position.End))],
						[],
						[]
					);
			}
		}

		if (children.Count is 0)
		{
			// GEO lists can't be empty!
			Position pos = twe.IsEmpty ? new(0, 0) : twe.First().Position.Start;
			range = range.ExpandTo(pos);

			return new ParseResult.Failed(
				position,
				range,
				Corrections: [new Edit.Insertion(pos, "\tPOINT (0/0)")],
				[],
				[]
			);
		}
		else
		{
			// The list is good if there are children, even if those children aren't.

			IEnumerable<IDrawableGeo> results = children
				.Where(static c => c is ParseResult<IDrawableGeo>)
				.Cast<ParseResult<IDrawableGeo>>()
				.Select(static r => r.Result);

			return new ParseResult<IDrawableGeo[]>(
				position,
				range,
				[.. results],
				[],
				[.. children]
			);
		}
	}

	public ParseResult ParseLocation(int position, ImmutableStack<Twe.Token[]> synchroSets)
	{
		List<Twe> literals = [];
		(var twes, position) = Twes(position);
		if (twes.FirstOrDefault(static twe => twe.Type is Name or Coordinate) is not Twe firstSection)
		{
			(bool shouldReturn, int newPos) = Synchro(position, synchroSets, Name, Coordinate);
			string skippedSegment = position is -1 || newPos is -1 ? "" : input[position..newPos];
			Range startRange = twes.IsEmpty ? new(new(0, 0), new(0, 0)) : new(twes.First().Position.Start, twes.First().Position.Start);
			(twes, position) = Twes(newPos);

			if (shouldReturn)
				return new ParseResult.Failed(
					position,
					startRange,
					[new Edit.Deletion(startRange)],
					[.. literals],
					[]
				);
			else if (twes.FirstOrDefault(static twe => twe.Type is Name or Coordinate) is Twe newFirstSection)
				firstSection = newFirstSection;
			else
				throw new Exception("Synchro failed!");
		}

		literals.Add(firstSection);
		Range range = firstSection.Position;

		// This could be a name or a coordinate. Figure it out and use it accordingly.
		PossiblyResolvedWaypoint operatingPoint = Parsing.ParseWaypoint(
			name: firstSection.Type is Name ? firstSection.Lexeme : null,
			coordinate: firstSection.Type is Coordinate ? firstSection.Lexeme : null,
			radialDist: null
		);

		position += firstSection.Lexeme.Length;
		(twes, position) = Twes(position);
		// If it was a name, check if a coordinate def follows WITHOUT A SPACE.
		if (firstSection.Type is Name && twes.FirstOrDefault(static twe => twe.Type is Coordinate) is Twe coordDef && firstSection.Position.End == coordDef.Position.Start)
		{
			// Add the definition in for later.
			literals.Add(coordDef);
			operatingPoint = Parsing.ParseWaypoint(firstSection.Lexeme, coordDef.Lexeme, null);
			_adHocFixes.Add(new(operatingPoint.FixName!.Name, operatingPoint));

			// Eat it and move on.
			position += coordDef.Lexeme.Length;
			(twes, position) = Twes(position);
			range = range with {
				End = coordDef.Position.End
			};
		}

		// Check if the fix is offset by a radial & distance.
		if (twes.FirstOrDefault(static twe => twe.Type is RadialDist) is Twe radial)
		{
			literals.Add(radial);
			operatingPoint = Parsing.ParseWaypoint(
				firstSection.Type is Name ? firstSection.Lexeme : null,
				firstSection.Type is Coordinate ? firstSection.Lexeme : null,
				radial.Lexeme
			);

			position += radial.Lexeme.Length;
			range = range with {
				End = radial.Position.End
			};
		}

		return new ParseResult<PossiblyResolvedWaypoint>(
			position,
			range,
			operatingPoint,
			Literals: [..literals],
			Children: []
		);
	}

	public ParseResult ParseLocationList(int position, ImmutableStack<Twe.Token[]> synchroSets)
	{
		List<ParseResult> children = [];
		ImmutableStack<Twe.Token[]> childSynchro = synchroSets.Push([Name, Coordinate]);

		for ((var twes, position) = Twes(position); twes.Any(static twe => twe.Type is Name or Coordinate); (twes, position) = Twes(position))
		{
			var child = ParseLocation(position, childSynchro);
			children.Add(child);
			position = child.NextIdx;
		}

		if (children.Count is 0)
		{
			// A location list can't be empty.
			Position pos = position is -1 ? new(0, 0) : _tweSet.LastOrDefault(kvp => kvp.Key <= position).Value.FirstOrDefault()?.Position.Start ?? new(0, 0);
			return new ParseResult.Failed(
				position,
				new(pos, pos),
				Corrections: [
					// Edit in an arbitrary identifier.
					new Edit.Insertion(pos, "FIX")
				],
				[],
				[]
			);
		}
		else
		{
			// The list is good, even if the children are bad!
			Range range = new(
				children.Min(static c => c.Range.Start)!,
				children.Max(static c => c.Range.End)!
			);

			return new ParseResult<PossiblyResolvedWaypoint[]>(
				children.Any(static c => c.NextIdx is -1) ? -1 : children.Max(static c => c.NextIdx),
				range,
				[.. children.Where(static c => c is ParseResult<PossiblyResolvedWaypoint>).Cast<ParseResult<PossiblyResolvedWaypoint>>().Select(static r => r.Result)],
				[],
				[.. children]
			);
		}
	}

	public (bool ShouldReturn, int NewPosition) Synchro(int position, ImmutableStack<Twe.Token[]> synchroSets, params Twe.Token[] expected)
	{
		// Set up for a synchro check.
		(var twes, position) = Twes(position);
		HashSet<Twe.Token> tokens = [.. twes.Select(static twe => twe.Type)];
		int endPoint = _tweSet.Keys.Max();

		// Eat the fewest tokens possible.
		while (position >= 0 && position <= endPoint)
		{
			if (tokens.Overlaps(expected))
				// Found a match! Yay! No return!
				return (false, position);
			else if (synchroSets.Any(tokens.Overlaps))
				// Ah well. Looks like we've gotta go back up the stack.
				return (true, position);

			// No luck; advance and try again.
			(twes, position) = Twes(position + 1);
			tokens = [.. twes.Select(static twe => twe.Type)];
		}

		// EOF. Pop and return the flag of doom.
		return (true, -1);
	}
}

public abstract record ParseResult(int NextIdx, Range Range, Twe[] Literals, ParseResult[] Children)
{
	public abstract IEnumerable<Edit> Edits { get; }


	public sealed record Failed(int NextIdx, Range Range, Edit[] Corrections, Twe[] Literals, ParseResult[] Children) : ParseResult(NextIdx, Range, Literals, Children)
	{
		public override IEnumerable<Edit> Edits => [.. Corrections, .. Children.SelectMany(static c => c.Edits)];

		public Failed Extend(IEnumerable<Edit> additionalCorrections) => this with {
			Corrections = [.. Corrections, .. additionalCorrections]
		};
	}
}

public sealed record ParseResult<T>(int NextIdx, Range Range, T Result, Twe[] Literals, ParseResult[] Children) : ParseResult(NextIdx, Range, Literals, Children)
{
	public override IEnumerable<Edit> Edits => [.. Children.SelectMany(static c => c.Edits)];
}

public abstract record Edit()
{
	public sealed record Insertion(Position Location, string Content) : Edit();
	public sealed record Deletion(Range Range) : Edit();
}
