using System.Collections.Immutable;
using System.Xml.Serialization;

namespace ManualAdjustments.LSP;

internal class FaaCycleData
{
	public ImmutableDictionary<string, ImmutableDictionary<string, string[]>> Charts { get; }

	public static async Task<string> GetCycleAsync()
	{
		HttpClient client = new();
		XmlSerializer serializer = new(typeof(productSet), [typeof(productSetStatus), typeof(productSetEdition)]);

		var productSet = (productSet)serializer.Deserialize(await client.GetStreamAsync("https://external-api.faa.gov/apra/dtpp/info"))!;
		return $"{productSet.edition.editionDate[^2..]}{productSet.edition.editionNumber:00}";
	}

	public static async Task<FaaCycleData> LoadAsync(string? cycle = null)
	{
		cycle ??= await GetCycleAsync();

		HttpClient client = new();
		XmlSerializer serializer = new(typeof(digital_tpp));

		digital_tpp tpp = (digital_tpp)serializer.Deserialize(await client.GetStreamAsync($"https://aeronav.faa.gov/d-tpp/{cycle}/xml_data/d-tpp_Metafile.xml"))!;
		return new(tpp);
	}

	private FaaCycleData(digital_tpp tpp)
	{
		Dictionary<string, ImmutableDictionary<string, string[]>> airports = [];

		(string Icao, digital_tppState_codeCity_nameAirport_nameRecord[] Records)[] recordSets = [..tpp.state_code
			.SelectMany(static state => state.city_name)
			.SelectMany(static city => city.airport_name)
			.Where(static ap => !string.IsNullOrWhiteSpace(ap.icao_ident))
			.Select(static ap => (ap.icao_ident, ap.record))];

		foreach (var (ap, records) in recordSets)
		{
			Dictionary<string, List<string>> urls = [];

			foreach (var record in records.Where(static r => r.chart_code is "DP" or "STAR" or "IAP"))
			{
				string[] procSegments = record.faanfd18.Split('.');
				string procName = procSegments.Length > 1 ? procSegments[1] : procSegments[0];
				string url = $"https://aeronav.faa.gov/d-tpp/{tpp.cycle}/{record.pdf_name}";

				if (string.IsNullOrWhiteSpace(procName))
					procName = record.pdf_name[5..^4];

				if (urls.TryGetValue(procName, out var existingSet))
					existingSet.Add(url);
				else
					urls.Add(procName, [url]);
			}

			if (urls.Count > 0)
				airports.Add(ap, urls.ToImmutableDictionary(static kvp => kvp.Key, static kvp => kvp.Value.ToArray()));
		}

		Charts = airports.ToImmutableDictionary();
	}
}

#nullable disable
#pragma warning disable IDE1006
// NOTE: Generated code may require at least .NET Framework 4.5 or .NET Core/Standard 2.0.
/// <remarks/>
[Serializable()]
[System.ComponentModel.DesignerCategory("code")]
[XmlType(AnonymousType = true, Namespace = "http://arpa.ait.faa.gov/arpa_response")]
[XmlRoot(Namespace = "http://arpa.ait.faa.gov/arpa_response", IsNullable = false)]
public partial class productSet
{

	private productSetStatus statusField;

	private productSetEdition editionField;

	/// <remarks/>
	public productSetStatus status
	{
		get
		{
			return statusField;
		}
		set
		{
			statusField = value;
		}
	}

	/// <remarks/>
	public productSetEdition edition
	{
		get
		{
			return editionField;
		}
		set
		{
			editionField = value;
		}
	}
}

/// <remarks/>
[Serializable()]
[System.ComponentModel.DesignerCategory("code")]
[XmlType(AnonymousType = true, Namespace = "http://arpa.ait.faa.gov/arpa_response")]
public partial class productSetStatus
{

	private byte codeField;

	private string messageField;

	/// <remarks/>
	[XmlAttribute()]
	public byte code
	{
		get
		{
			return codeField;
		}
		set
		{
			codeField = value;
		}
	}

	/// <remarks/>
	[XmlAttribute()]
	public string message
	{
		get
		{
			return messageField;
		}
		set
		{
			messageField = value;
		}
	}
}

/// <remarks/>
[Serializable()]
[System.ComponentModel.DesignerCategory("code")]
[XmlType(AnonymousType = true, Namespace = "http://arpa.ait.faa.gov/arpa_response")]
public partial class productSetEdition
{

	private string editionDateField;

	private byte editionNumberField;

	private string geonameField;

	private string editionNameField;

	private string formatField;

	/// <remarks/>
	public string editionDate
	{
		get
		{
			return editionDateField;
		}
		set
		{
			editionDateField = value;
		}
	}

	/// <remarks/>
	public byte editionNumber
	{
		get
		{
			return editionNumberField;
		}
		set
		{
			editionNumberField = value;
		}
	}

	/// <remarks/>
	[XmlAttribute()]
	public string geoname
	{
		get
		{
			return geonameField;
		}
		set
		{
			geonameField = value;
		}
	}

	/// <remarks/>
	[XmlAttribute()]
	public string editionName
	{
		get
		{
			return editionNameField;
		}
		set
		{
			editionNameField = value;
		}
	}

	/// <remarks/>
	[XmlAttribute()]
	public string format
	{
		get
		{
			return formatField;
		}
		set
		{
			formatField = value;
		}
	}
}


// NOTE: Generated code may require at least .NET Framework 4.5 or .NET Core/Standard 2.0.
/// <remarks/>
[Serializable()]
[System.ComponentModel.DesignerCategory("code")]
[XmlType(AnonymousType = true)]
[XmlRoot(Namespace = "", IsNullable = false)]
public partial class digital_tpp
{

	private digital_tppState_code[] state_codeField;

	private ushort cycleField;

	private string from_edateField;

	private string to_edateField;

	/// <remarks/>
	[XmlElement("state_code")]
	public digital_tppState_code[] state_code
	{
		get
		{
			return state_codeField;
		}
		set
		{
			state_codeField = value;
		}
	}

	/// <remarks/>
	[XmlAttribute()]
	public ushort cycle
	{
		get
		{
			return cycleField;
		}
		set
		{
			cycleField = value;
		}
	}

	/// <remarks/>
	[XmlAttribute()]
	public string from_edate
	{
		get
		{
			return from_edateField;
		}
		set
		{
			from_edateField = value;
		}
	}

	/// <remarks/>
	[XmlAttribute()]
	public string to_edate
	{
		get
		{
			return to_edateField;
		}
		set
		{
			to_edateField = value;
		}
	}
}

/// <remarks/>
[Serializable()]
[System.ComponentModel.DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public partial class digital_tppState_code
{

	private digital_tppState_codeCity_name[] city_nameField;

	private string idField;

	private string state_fullnameField;

	/// <remarks/>
	[XmlElement("city_name")]
	public digital_tppState_codeCity_name[] city_name
	{
		get
		{
			return city_nameField;
		}
		set
		{
			city_nameField = value;
		}
	}

	/// <remarks/>
	[XmlAttribute()]
	public string ID
	{
		get
		{
			return idField;
		}
		set
		{
			idField = value;
		}
	}

	/// <remarks/>
	[XmlAttribute()]
	public string state_fullname
	{
		get
		{
			return state_fullnameField;
		}
		set
		{
			state_fullnameField = value;
		}
	}
}

/// <remarks/>
[Serializable()]
[System.ComponentModel.DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public partial class digital_tppState_codeCity_name
{

	private digital_tppState_codeCity_nameAirport_name[] airport_nameField;

	private string idField;

	private string volumeField;

	/// <remarks/>
	[XmlElement("airport_name")]
	public digital_tppState_codeCity_nameAirport_name[] airport_name
	{
		get
		{
			return airport_nameField;
		}
		set
		{
			airport_nameField = value;
		}
	}

	/// <remarks/>
	[XmlAttribute()]
	public string ID
	{
		get
		{
			return idField;
		}
		set
		{
			idField = value;
		}
	}

	/// <remarks/>
	[XmlAttribute()]
	public string volume
	{
		get
		{
			return volumeField;
		}
		set
		{
			volumeField = value;
		}
	}
}

/// <remarks/>
[Serializable()]
[System.ComponentModel.DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public partial class digital_tppState_codeCity_nameAirport_name
{

	private digital_tppState_codeCity_nameAirport_nameRecord[] recordField;

	private string idField;

	private string militaryField;

	private string apt_identField;

	private string icao_identField;

	private ushort alnumField;

	/// <remarks/>
	[XmlElement("record")]
	public digital_tppState_codeCity_nameAirport_nameRecord[] record
	{
		get
		{
			return recordField;
		}
		set
		{
			recordField = value;
		}
	}

	/// <remarks/>
	[XmlAttribute()]
	public string ID
	{
		get
		{
			return idField;
		}
		set
		{
			idField = value;
		}
	}

	/// <remarks/>
	[XmlAttribute()]
	public string military
	{
		get
		{
			return militaryField;
		}
		set
		{
			militaryField = value;
		}
	}

	/// <remarks/>
	[XmlAttribute()]
	public string apt_ident
	{
		get
		{
			return apt_identField;
		}
		set
		{
			apt_identField = value;
		}
	}

	/// <remarks/>
	[XmlAttribute()]
	public string icao_ident
	{
		get
		{
			return icao_identField;
		}
		set
		{
			icao_identField = value;
		}
	}

	/// <remarks/>
	[XmlAttribute()]
	public ushort alnum
	{
		get
		{
			return alnumField;
		}
		set
		{
			alnumField = value;
		}
	}
}

/// <remarks/>
[Serializable()]
[System.ComponentModel.DesignerCategory("code")]
[XmlType(AnonymousType = true)]
public partial class digital_tppState_codeCity_nameAirport_nameRecord
{

	private uint chartseqField;

	private string chart_codeField;

	private string chart_nameField;

	private string useractionField;

	private string pdf_nameField;

	private string cn_flgField;

	private string cnsectionField;

	private string cnpageField;

	private string bvsectionField;

	private string bvpageField;

	private string procuidField;

	private string two_coloredField;

	private string civilField;

	private string faanfd18Field;

	private string copterField;

	private string amdtnumField;

	private string amdtdateField;

	/// <remarks/>
	public uint chartseq
	{
		get
		{
			return chartseqField;
		}
		set
		{
			chartseqField = value;
		}
	}

	/// <remarks/>
	public string chart_code
	{
		get
		{
			return chart_codeField;
		}
		set
		{
			chart_codeField = value;
		}
	}

	/// <remarks/>
	public string chart_name
	{
		get
		{
			return chart_nameField;
		}
		set
		{
			chart_nameField = value;
		}
	}

	/// <remarks/>
	public string useraction
	{
		get
		{
			return useractionField;
		}
		set
		{
			useractionField = value;
		}
	}

	/// <remarks/>
	public string pdf_name
	{
		get
		{
			return pdf_nameField;
		}
		set
		{
			pdf_nameField = value;
		}
	}

	/// <remarks/>
	public string cn_flg
	{
		get
		{
			return cn_flgField;
		}
		set
		{
			cn_flgField = value;
		}
	}

	/// <remarks/>
	public string cnsection
	{
		get
		{
			return cnsectionField;
		}
		set
		{
			cnsectionField = value;
		}
	}

	/// <remarks/>
	public string cnpage
	{
		get
		{
			return cnpageField;
		}
		set
		{
			cnpageField = value;
		}
	}

	/// <remarks/>
	public string bvsection
	{
		get
		{
			return bvsectionField;
		}
		set
		{
			bvsectionField = value;
		}
	}

	/// <remarks/>
	public string bvpage
	{
		get
		{
			return bvpageField;
		}
		set
		{
			bvpageField = value;
		}
	}

	/// <remarks/>
	public string procuid
	{
		get
		{
			return procuidField;
		}
		set
		{
			procuidField = value;
		}
	}

	/// <remarks/>
	public string two_colored
	{
		get
		{
			return two_coloredField;
		}
		set
		{
			two_coloredField = value;
		}
	}

	/// <remarks/>
	public string civil
	{
		get
		{
			return civilField;
		}
		set
		{
			civilField = value;
		}
	}

	/// <remarks/>
	public string faanfd18
	{
		get
		{
			return faanfd18Field;
		}
		set
		{
			faanfd18Field = value;
		}
	}

	/// <remarks/>
	public string copter
	{
		get
		{
			return copterField;
		}
		set
		{
			copterField = value;
		}
	}

	/// <remarks/>
	public string amdtnum
	{
		get
		{
			return amdtnumField;
		}
		set
		{
			amdtnumField = value;
		}
	}

	/// <remarks/>
	public string amdtdate
	{
		get
		{
			return amdtdateField;
		}
		set
		{
			amdtdateField = value;
		}
	}
}
#pragma warning restore
#nullable restore
