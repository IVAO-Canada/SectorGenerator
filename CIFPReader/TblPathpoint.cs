using System;
using System.Collections.Generic;

namespace CIFPReader;

public partial class TblPathpoint
{
    public string? AreaCode { get; set; }

    public string? AirportIdentifier { get; set; }

    public string? IcaoCode { get; set; }

    public string? ApproachProcedureIdent { get; set; }

    public string? RunwayIdentifier { get; set; }

    public int? SbasServiceProviderIdentifier { get; set; }

    public string? ReferencePathIdentifier { get; set; }

    public double? LandingThresholdLatitude { get; set; }

    public double? LandingThresholdLongitude { get; set; }

    public double? LtpEllipsoidHeight { get; set; }

    public double? GlidepathAngle { get; set; }

    public double? FlightpathAlignmentLatitude { get; set; }

    public double? FlightpathAlignmentLongitude { get; set; }

    public double? CourseWidthAtThreshold { get; set; }

    public int? LengthOffset { get; set; }

    public int? PathPointTch { get; set; }

    public string? TchUnitsIndicator { get; set; }

    public int? Hal { get; set; }

    public int? Val { get; set; }

    public double? FpapEllipsoidHeight { get; set; }

    public double? FpapOrthometricHeight { get; set; }

    public double? LtpOrthometricHeight { get; set; }

    public string? ApproachTypeIdentifier { get; set; }

    public int? GnssChannelNumber { get; set; }
}
