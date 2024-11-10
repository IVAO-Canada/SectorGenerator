using System;
using System.Collections.Generic;

namespace CIFPReader;

public partial class TblLocalizersGlideslope
{
    public string? AreaCode { get; set; }

    public string? IcaoCode { get; set; }

    public string AirportIdentifier { get; set; } = null!;

    public string? RunwayIdentifier { get; set; }

    public string LlzIdentifier { get; set; } = null!;

    public double? LlzLatitude { get; set; }

    public double? LlzLongitude { get; set; }

    public double? LlzFrequency { get; set; }

    public double? LlzBearing { get; set; }

    public double? LlzWidth { get; set; }

    public string? IlsMlsGlsCategory { get; set; }

    public double? GsLatitude { get; set; }

    public double? GsLongitude { get; set; }

    public double? GsAngle { get; set; }

    public int? GsElevation { get; set; }

    public double? StationDeclination { get; set; }

    public string? Id { get; set; }
}
