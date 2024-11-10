using System;
using System.Collections.Generic;

namespace CIFPReader;

public partial class TblHeader
{
    public decimal Version { get; set; }

    public string Arincversion { get; set; } = null!;

    public string RecordSet { get; set; } = null!;

    public string CurrentAirac { get; set; } = null!;

    public string Revision { get; set; } = null!;

    public string EffectiveFromto { get; set; } = null!;

    public string PreviousAirac { get; set; } = null!;

    public string PreviousFromto { get; set; } = null!;

    public string ParsedAt { get; set; } = null!;
}
