using System;
using System.Collections.Generic;

namespace CIFPReader;

public partial class TblStar
{
    public string? AreaCode { get; set; }

    public string AirportIdentifier { get; set; } = "";

    public string ProcedureIdentifier { get; set; } = "";

    public char? RouteType { get; set; }

    public string? TransitionIdentifier { get; set; }

    public int? Seqno { get; set; }

    public string? WaypointIcaoCode { get; set; }

    public string? WaypointIdentifier { get; set; }

    public decimal? WaypointLatitude { get; set; }

    public decimal? WaypointLongitude { get; set; }

    public string? WaypointDescriptionCode { get; set; }

    public string? TurnDirection { get; set; }

    public double? Rnp { get; set; }

    public string? PathTermination { get; set; }

    public string? RecommandedNavaid { get; set; }

    public decimal? RecommandedNavaidLatitude { get; set; }

    public decimal? RecommandedNavaidLongitude { get; set; }

    public double? ArcRadius { get; set; }

    public double? Theta { get; set; }

    public double? Rho { get; set; }

    public double? MagneticCourse { get; set; }

    public double? RouteDistanceHoldingDistanceTime { get; set; }

    public string? DistanceTime { get; set; }

    public char? AltitudeDescription { get; set; }

    public int? Altitude1 { get; set; }

    public int? Altitude2 { get; set; }

    public int? TransitionAltitude { get; set; }

    public char? SpeedLimitDescription { get; set; }

    public uint? SpeedLimit { get; set; }

    public double? VerticalAngle { get; set; }

    public string? CenterWaypoint { get; set; }

    public double? CenterWaypointLatitude { get; set; }

    public double? CenterWaypointLongitude { get; set; }

    public string? AircraftCategory { get; set; }

    public string? Id { get; set; }

    public string? RecommandedId { get; set; }

    public string? CenterId { get; set; }
}
