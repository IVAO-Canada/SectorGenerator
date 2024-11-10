using System;
using System.Collections.Generic;

namespace CIFPReader;

public partial class TblVhfnavaid
{
    public string? AreaCode { get; set; }

    public string? AirportIdentifier { get; set; }

    public string IcaoCode { get; set; } = null!;

    public string VorIdentifier { get; set; } = null!;

    public string? VorName { get; set; }

    public decimal? VorFrequency { get; set; }

    public string NavaidClass { get; set; }

    public decimal? VorLatitude { get; set; }

    public decimal? VorLongitude { get; set; }

    public string? DmeIdent { get; set; }

    public decimal? DmeLatitude { get; set; }

    public decimal? DmeLongitude { get; set; }

    public int? DmeElevation { get; set; }

    public decimal? IlsdmeBias { get; set; }

    public int? Range { get; set; }

    public decimal? StationDeclination { get; set; }

    public decimal? MagneticVariation { get; set; }

    public string? Id { get; set; }
}
