using System;
using System.Collections.Generic;

namespace CIFPReader;

public partial class TblGl
{
    public string? AreaCode { get; set; }

    public string? AirportIdentifier { get; set; }

    public string? IcaoCode { get; set; }

    public string? GlsRefPathIdentifier { get; set; }

    public string? GlsCategory { get; set; }

    public int? GlsChannel { get; set; }

    public string? RunwayIdentifier { get; set; }

    public double? GlsApproachBearing { get; set; }

    public double? StationLatitude { get; set; }

    public double? StationLongitude { get; set; }

    public string? GlsStationIdent { get; set; }

    public double? GlsApproachSlope { get; set; }

    public double? MagenticVariation { get; set; }

    public int? StationElevation { get; set; }

    public string? StationType { get; set; }

    public string? Id { get; set; }
}
