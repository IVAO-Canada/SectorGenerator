using System;
using System.Collections.Generic;

namespace CIFPReader;

public partial class TblEnrouteCommunication
{
    public string? AreaCode { get; set; }

    public string? FirRdoIdent { get; set; }

    public string? FirUirIndicator { get; set; }

    public string? CommunicationType { get; set; }

    public double? CommunicationFrequency { get; set; }

    public string? FrequencyUnits { get; set; }

    public string? ServiceIndicator { get; set; }

    public string? RemoteName { get; set; }

    public string? Callsign { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }
}
