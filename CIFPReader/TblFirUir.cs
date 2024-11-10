using System;
using System.Collections.Generic;

namespace CIFPReader;

public partial class TblFirUir
{
    public string? AreaCode { get; set; }

    public string FirUirIdentifier { get; set; } = "";

    public string? FirUirAddress { get; set; }

    public string? FirUirName { get; set; }

    public string? FirUirIndicator { get; set; }

    public int Seqno { get; set; }

    public string? BoundaryVia { get; set; }

    public string? AdjacentFirIdentifier { get; set; }

    public string? AdjacentUirIdentifier { get; set; }

    public int? ReportingUnitsSpeed { get; set; }

    public int? ReportingUnitsAltitude { get; set; }

    public double? FirUirLatitude { get; set; }

    public double? FirUirLongitude { get; set; }

    public decimal? ArcOriginLatitude { get; set; }

    public decimal? ArcOriginLongitude { get; set; }

    public decimal? ArcDistance { get; set; }

    public decimal? ArcBearing { get; set; }

    public string? FirUpperLimit { get; set; }

    public string? UirLowerLimit { get; set; }

    public string? UirUpperLimit { get; set; }

    public string? CruiseTableIdentifier { get; set; }
}
