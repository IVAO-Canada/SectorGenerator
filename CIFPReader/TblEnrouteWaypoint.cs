using System;
using System.Collections.Generic;

namespace CIFPReader;

public partial class TblEnrouteWaypoint
{
    public string? AreaCode { get; set; }

    public string IcaoCode { get; set; } = null!;

    public string WaypointIdentifier { get; set; } = null!;

    public string? WaypointName { get; set; }

    public string? WaypointType { get; set; }

    public string? WaypointUsage { get; set; }

    public double? WaypointLatitude { get; set; }

    public double? WaypointLongitude { get; set; }

    public string? Id { get; set; }
}
