using System;
using System.Collections.Generic;

namespace CIFPReader;

public partial class TblEnrouteAirway
{
    public string? AreaCode { get; set; }

    public string RouteIdentifier { get; set; } = "";

    public int Seqno { get; set; }

    public string? IcaoCode { get; set; }

    public string WaypointIdentifier { get; set; } = "";

    public decimal WaypointLatitude { get; set; }

    public decimal WaypointLongitude { get; set; }

    public string? WaypointDescriptionCode { get; set; }

    public char RouteType { get; set; }

    public char Flightlevel { get; set; }

    public string? DirectionRestriction { get; set; }

    public string? CrusingTableIdentifier { get; set; }

    public int? MinimumAltitude1 { get; set; }

    public int? MinimumAltitude2 { get; set; }

    public int? MaximumAltitude { get; set; }

    public decimal? OutboundCourse { get; set; }

    public decimal? InboundCourse { get; set; }

    public decimal? InboundDistance { get; set; }

    public string? Id { get; set; }
}
