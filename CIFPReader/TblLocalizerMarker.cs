using System;
using System.Collections.Generic;

namespace CIFPReader;

public partial class TblLocalizerMarker
{
    public string AreaCode { get; set; } = null!;

    public string IcaoCode { get; set; } = null!;

    public string AirportIdentifier { get; set; } = null!;

    public string RunwayIdentifier { get; set; } = null!;

    public string LlzIdentifier { get; set; } = null!;

    public string MarkerIdentifier { get; set; } = null!;

    public string MarkerType { get; set; } = null!;

    public double MarkerLatitude { get; set; }

    public double MarkerLongitude { get; set; }

    public string? Id { get; set; }
}
