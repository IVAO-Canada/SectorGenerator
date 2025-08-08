namespace ManualAdjustments;

public abstract partial record ManualAdjustment;

public abstract record ManualAddition : ManualAdjustment;

public abstract record ManualDeletion : ManualAdjustment;

public record AddFix(string Name, PossiblyResolvedWaypoint Position) : ManualAddition;
public sealed record AddVfrFix(string Name, PossiblyResolvedWaypoint Position) : AddFix(Name, Position);
public sealed record RemoveFix(PossiblyResolvedWaypoint Fix) : ManualDeletion;

public sealed record AddVfrRoute(string Filter, PossiblyResolvedWaypoint[] Points) : ManualAddition;

public sealed record AddAirway(PossiblyResolvedWaypoint[] Points, AddAirway.AirwayType Type, string Identifier) : ManualAddition
{
	public enum AirwayType
	{
		High,
		Low
	}
}

public sealed record RemoveAirway(AddAirway.AirwayType Type, string Identifier) : ManualDeletion;

public sealed record AddGeo(string Tag, string Colour, IDrawableGeo[] Geos) : ManualAddition;

public sealed record AddProcedure(string Airport, AddProcedure.ProcedureType Type, string Identifier, IDrawableGeo[] Geos) : ManualAddition
{
	public enum ProcedureType
	{
		SID,
		STAR,
		IAP
	}
}

public sealed record RemoveProcedure(string Airport, AddProcedure.ProcedureType Type, string Identifier) : ManualDeletion;
