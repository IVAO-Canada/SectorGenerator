using System;
using System.Collections.Generic;

namespace CIFPReader;

public partial class TblRunway
{
    public string? AreaCode { get; set; }

    public string? IcaoCode { get; set; }

    public string AirportIdentifier { get; set; } = null!;

    public string RunwayIdentifier { get; set; } = null!;

    public decimal RunwayLatitude { get; set; }

    public decimal RunwayLongitude { get; set; }

    public double? RunwayGradient { get; set; }

    public decimal RunwayMagneticBearing { get; set; }

    public decimal RunwayTrueBearing { get; set; }

    public int? LandingThresholdElevation { get; set; }

    public int? DisplacedThresholdDistance { get; set; }

    public int? ThresholdCrossingHeight { get; set; }

    public uint? RunwayLength { get; set; }

    public uint? RunwayWidth { get; set; }

    public string? LlzIdentifier { get; set; }

    public char? LlzMlsGlsCategory { get; set; }

    public int? SurfaceCode { get; set; }

    public string? Id { get; set; }
}
