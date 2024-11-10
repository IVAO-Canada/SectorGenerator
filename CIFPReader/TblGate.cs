using System;
using System.Collections.Generic;

namespace CIFPReader;

public partial class TblGate
{
    public string? AreaCode { get; set; }

    public string? AirportIdentifier { get; set; }

    public string? IcaoCode { get; set; }

    public string? GateIdentifier { get; set; }

    public double? GateLatitude { get; set; }

    public double? GateLongitude { get; set; }

    public string? Name { get; set; }
}
