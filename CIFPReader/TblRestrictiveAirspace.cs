using System;
using System.Collections.Generic;

namespace CIFPReader;

public partial class TblRestrictiveAirspace
{
    public string? AreaCode { get; set; }

    public string? IcaoCode { get; set; }

    public string? RestrictiveAirspaceDesignation { get; set; }

    public string? RestrictiveAirspaceName { get; set; }

    public string? RestrictiveType { get; set; }

    public string? MultipleCode { get; set; }

    public int? Seqno { get; set; }

    public string? BoundaryVia { get; set; }

    public string? Flightlevel { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public double? ArcOriginLatitude { get; set; }

    public double? ArcOriginLongitude { get; set; }

    public double? ArcDistance { get; set; }

    public double? ArcBearing { get; set; }

    public string? UnitIndicatorLowerLimit { get; set; }

    public string? LowerLimit { get; set; }

    public string? UnitIndicatorUpperLimit { get; set; }

    public string? UpperLimit { get; set; }
}
