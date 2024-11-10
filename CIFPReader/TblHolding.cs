using System;
using System.Collections.Generic;

namespace CIFPReader;

public partial class TblHolding
{
    public string? AreaCode { get; set; }

    public string? RegionCode { get; set; }

    public string? IcaoCode { get; set; }

    public string? WaypointIdentifier { get; set; }

    public string? HoldingName { get; set; }

    public double? WaypointLatitude { get; set; }

    public double? WaypointLongitude { get; set; }

    public int? DuplicateIdentifier { get; set; }

    public double? InboundHoldingCourse { get; set; }

    public string? TurnDirection { get; set; }

    public double? LegLength { get; set; }

    public double? LegTime { get; set; }

    public int? MinimumAltitude { get; set; }

    public int? MaximumAltitude { get; set; }

    public int? HoldingSpeed { get; set; }
}
