using CIFPReader;

namespace ManualAdjustments.LSP.Types.Semantics;

internal abstract record SemanticTree(Range Range);

internal sealed record File(Adjustment[] Adjustments, Range Range) : SemanticTree(Range)
{
	public static File Construct(ParseResult parse)
	{
		CIFP cifp = InjectionContext.Shared.Get<CIFP>();

		if (parse is ParseResult<ManualAdjustment[]> adjustmentParse)
			foreach (ManualAdjustment fix in adjustmentParse.Result.Where(r => r is AddFix or AddVfrFix))
			{
				string name;
				PossiblyResolvedWaypoint pos;

				if (fix is AddFix af)
					(name, pos) = (af.Name, af.Position);
				else if (fix is AddVfrFix avf)
					(name, pos) = (avf.Name, avf.Position);
				else
					throw new System.Diagnostics.UnreachableException();

				ICoordinate resolvedPos = pos.Resolve(cifp);

				if (cifp.Fixes.TryGetValue(name, out var coords))
					coords.Add(resolvedPos);
				else
					cifp.Fixes.Add(name, [resolvedPos]);
			}

		return new(
			[.. parse.Children.Where(static c => c.GetType().IsGenericType).Select(static c => Adjustment.Construct(c))],
			parse.Range
		);
	}
}

internal abstract record Adjustment(Range Range) : SemanticTree(Range)
{
	public static Adjustment Construct(ParseResult parse)
	{
		if (parse is ParseResult<AddFix> addFix)
			return FixDefinition.Construct(addFix);
		else if (parse is ParseResult<RemoveFix> remFix)
			return FixDeletion.Construct(remFix);
		else if (parse is ParseResult<AddAirway> awDef)
			return AirwayDefinition.Construct(awDef);
		else if (parse is ParseResult<AddVfrFix> addVfrFix)
			return VfrFixDefinition.Construct(addVfrFix);
		else if (parse is ParseResult<AddVfrRoute> addVfrRoute)
			return VfrRouteDefinition.Construct(addVfrRoute);
		else if (parse is ParseResult<AddGeo> addGeo)
			return GeoDefinition.Construct(addGeo);
		else if (parse is ParseResult<AddProcedure> addProc)
			return ProcedureDefinition.Construct(addProc);
		else
			throw new ArgumentException("Unknown parse result type.", nameof(parse));
	}
}

internal sealed record FixDefinition(Location Fix, Range Range) : Adjustment(Range)
{
	public static FixDefinition Construct(ParseResult<AddFix> parse)
	{
		CIFP cifp = InjectionContext.Shared.Get<CIFP>();
		NamedCoordinate fix = new(parse.Result.Name.Trim('"'), parse.Result.Position.Resolve(cifp).GetCoordinate());
		return new(new(fix, parse.Children[0].Range), parse.Range);
	}
}

internal sealed record FixDeletion(string Name, Range Range) : Adjustment(Range)
{
	public static FixDeletion Construct(ParseResult<RemoveFix> parse) => new(
		parse.Result.Fix.FixName!.Name.Trim('"'),
		parse.Range
	);
}

internal sealed record AirwayDefinition(string Name, Location[] Points, AddAirway.AirwayType Type, Range Range) : Adjustment(Range)
{
	public static AirwayDefinition Construct(ParseResult<AddAirway> parse)
	{
		CIFP cifp = InjectionContext.Shared.Get<CIFP>();

		if (parse.Children[0] is not ParseResult<PossiblyResolvedWaypoint[]> locationList)
			throw new ArgumentException("Invalid definition.", nameof(parse));

		return new(
			parse.Result.Identifier.Trim('"'),
			[.. locationList.Children.Cast<ParseResult<PossiblyResolvedWaypoint>>().Select(c => Location.Construct(c, cifp))],
			parse.Result.Type,
			parse.Range
		);
	}
}

internal sealed record VfrFixDefinition(Location Fix, Range Range) : Adjustment(Range)
{
	public static VfrFixDefinition Construct(ParseResult<AddVfrFix> parse)
	{
		CIFP cifp = InjectionContext.Shared.Get<CIFP>();
		NamedCoordinate fix = new(parse.Result.Name.Trim('"'), parse.Result.Position.Resolve(cifp).GetCoordinate());
		return new(new(fix, parse.Children[0].Range), parse.Range);
	}
}

internal sealed record VfrRouteDefinition(string Filter, Location[] Points, Range Range) : Adjustment(Range)
{
	public static VfrRouteDefinition Construct(ParseResult<AddVfrRoute> parse)
	{
		CIFP cifp = InjectionContext.Shared.Get<CIFP>();

		if (parse.Children[0] is not ParseResult<PossiblyResolvedWaypoint[]> locationList)
			throw new ArgumentException("Invalid definition.", nameof(parse));

		return new(
			parse.Result.Filter,
			[.. locationList.Children.Cast<ParseResult<PossiblyResolvedWaypoint>>().Select(c => Location.Construct(c, cifp))],
			parse.Range
		);
	}
}

internal sealed record GeoDefinition(string Name, Color Colour, LspGeo[] Geos, Range Range) : Adjustment(Range)
{
	public static GeoDefinition Construct(ParseResult<AddGeo> parse)
	{
		CIFP cifp = InjectionContext.Shared.Get<CIFP>();
		var validChildren = parse.Children.Where(static c => c is ParseResult<IDrawableGeo>).Cast<ParseResult<IDrawableGeo>>();

		return new(
			parse.Result.Tag,
			Color.ParseAurora(parse.Result.Colour),
			[.. validChildren.Select(c => LspGeo.Construct(c, cifp))],
			parse.Range
		);
	}
}

internal sealed record ProcedureDefinition(string Airport, AddProcedure.ProcedureType Type, string Name, LspGeo[] Geos, Range AirportRange, Range NameRange, Range Range) : Adjustment(Range)
{
	public static ProcedureDefinition Construct(ParseResult<AddProcedure> parse)
	{
		CIFP cifp = InjectionContext.Shared.Get<CIFP>();

		if (parse.Children[^1] is not ParseResult<IDrawableGeo[]> geos)
			return new(
				parse.Result.Airport,
				parse.Result.Type,
				parse.Result.Identifier.Trim('"'),
				[],
				parse.Children[0].Range,
				parse.Children[1].Range,
				parse.Range
			);

		var validChildren = geos.Children.Where(static c => c is ParseResult<IDrawableGeo>).Cast<ParseResult<IDrawableGeo>>();

		return new(
			parse.Result.Airport,
			parse.Result.Type,
			parse.Result.Identifier.Trim('"'),
			[.. validChildren.Select(c => LspGeo.Construct(c, cifp))],
			parse.Children[0].Range,
			parse.Children[1].Range,
			parse.Range
		);
	}
}

internal sealed record Location(ICoordinate Coordinate, Range Range)
{
	public static Location Construct(ParseResult<PossiblyResolvedWaypoint> parse) => Construct(parse, InjectionContext.Shared.Get<CIFP>());
	public static Location Construct(ParseResult<PossiblyResolvedWaypoint> parse, CIFP cifp) => new(
		parse.Result.Resolve(cifp),
		parse.Range
	);
}

internal sealed record LspGeo(IDrawableGeo Geo, Location[] References, Range Range)
{
	public static LspGeo Construct(ParseResult<IDrawableGeo> parse, CIFP cifp)
	{
		Location[] references;

		if (parse.Children[0] is ParseResult<PossiblyResolvedWaypoint> symbolPoint)
			references = [Location.Construct(symbolPoint, cifp)];
		else if (parse.Children[0] is ParseResult<PossiblyResolvedWaypoint[]> connectorPoints)
			references = [..
				connectorPoints.Children
					.Where(static c => c is ParseResult<PossiblyResolvedWaypoint>)
					.Cast<ParseResult<PossiblyResolvedWaypoint>>()
					.Select(c => Location.Construct(c, cifp))
			];
		else
			throw new ArgumentException("Can't construct a GEO from a series of failed points!", nameof(parse));

		return new(
			parse.Result,
			references,
			parse.Range
		);
	}
}
