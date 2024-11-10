using System;
using System.Collections.Generic;

namespace CIFPReader;

public partial class TblControlledAirspace
{
    public string? AreaCode { get; set; }

    public string? IcaoCode { get; set; }

    public string? AirspaceCenter { get; set; }

    public string? ControlledAirspaceName { get; set; }

    public string? AirspaceType { get; set; }

    public string? AirspaceClassification { get; set; }

    public char? MultipleCode { get; set; }

    public string? TimeCode { get; set; }

    public uint? Seqno { get; set; }

    public string? Flightlevel { get; set; }

    public string BoundaryVia { get; set; } = "";

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public decimal? ArcOriginLatitude { get; set; }

    public decimal? ArcOriginLongitude { get; set; }

    public decimal? ArcDistance { get; set; }

    public decimal? ArcBearing { get; set; }

    public string? UnitIndicatorLowerLimit { get; set; }

    public string? LowerLimit { get; set; }

    public string? UnitIndicatorUpperLimit { get; set; }

    public string? UpperLimit { get; set; }
}
