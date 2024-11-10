using System;
using System.Collections.Generic;

namespace CIFPReader;

public partial class TblCruisingTable
{
    public string? CruiseTableIdentifier { get; set; }

    public int? Seqno { get; set; }

    public double? CourseFrom { get; set; }

    public double? CourseTo { get; set; }

    public string? MagTrue { get; set; }

    public int? CruiseLevelFrom1 { get; set; }

    public int? VerticalSeparation1 { get; set; }

    public int? CruiseLevelTo1 { get; set; }

    public int? CruiseLevelFrom2 { get; set; }

    public int? VerticalSeparation2 { get; set; }

    public int? CruiseLevelTo2 { get; set; }

    public int? CruiseLevelFrom3 { get; set; }

    public int? VerticalSeparation3 { get; set; }

    public int? CruiseLevelTo3 { get; set; }

    public int? CruiseLevelFrom4 { get; set; }

    public int? VerticalSeparation4 { get; set; }

    public int? CruiseLevelTo4 { get; set; }
}
