using System;
using System.Collections.Generic;

namespace CIFPReader;

public partial class TblAirportMsa
{
    public string? AreaCode { get; set; }

    public string? IcaoCode { get; set; }

    public string? AirportIdentifier { get; set; }

    public string? MsaCenter { get; set; }

    public double? MsaCenterLatitude { get; set; }

    public double? MsaCenterLongitude { get; set; }

    public string? MagneticTrueIndicator { get; set; }

    public string? MultipleCode { get; set; }

    public int? RadiusLimit { get; set; }

    public int? SectorBearing1 { get; set; }

    public int? SectorAltitude1 { get; set; }

    public int? SectorBearing2 { get; set; }

    public int? SectorAltitude2 { get; set; }

    public int? SectorBearing3 { get; set; }

    public int? SectorAltitude3 { get; set; }

    public int? SectorBearing4 { get; set; }

    public int? SectorAltitude4 { get; set; }

    public int? SectorBearing5 { get; set; }

    public int? SectorAltitude5 { get; set; }
}
