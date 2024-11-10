using System;
using System.Collections.Generic;

namespace CIFPReader;

public partial class TblAirport
{
    public string? AreaCode { get; set; }

    public string IcaoCode { get; set; } = null!;

    public string AirportIdentifier { get; set; } = null!;

    public string? AirportIdentifier3letter { get; set; }

    public string? AirportName { get; set; }

    public decimal AirportRefLatitude { get; set; }

    public decimal AirportRefLongitude { get; set; }

    public string? IfrCapability { get; set; }

    public string? LongestRunwaySurfaceCode { get; set; }

    public int Elevation { get; set; }

    public int? TransitionAltitude { get; set; }

    public int? TransitionLevel { get; set; }

    public int? SpeedLimit { get; set; }

    public int? SpeedLimitAltitude { get; set; }

    public string? IataAtaDesignator { get; set; }

    public string? Id { get; set; }
}
