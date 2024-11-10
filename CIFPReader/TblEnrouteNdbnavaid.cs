using System;
using System.Collections.Generic;

namespace CIFPReader;

public partial class TblEnrouteNdbnavaid
{
    public string? AreaCode { get; set; }

    public string IcaoCode { get; set; } = null!;

    public string NdbIdentifier { get; set; } = null!;

    public string? NdbName { get; set; }

    public double? NdbFrequency { get; set; }

    public string? NavaidClass { get; set; }

    public double? NdbLatitude { get; set; }

    public double? NdbLongitude { get; set; }

    public int? Range { get; set; }

    public string? Id { get; set; }
}
