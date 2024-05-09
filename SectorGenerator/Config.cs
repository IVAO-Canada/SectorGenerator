using System.Text.Json.Serialization;

namespace SectorGenerator;

public struct Config
{
	public string OutputFolder { get; set; }

	public string PbfPath { get; set; }

	public string? BoundaryFilePath { get; set; }

	public string? IvaoApiToken { get; set; }

	public Dictionary<string, string[]> SectorAdditionalAirports { get; set; }

	[JsonIgnore]
	public static Config Default => new() {
		OutputFolder = @"C:\Program Files\IVAO\Aurora\SectorFiles",
		PbfPath = @".\us-latest.osm.pbf",
		BoundaryFilePath = null,
		IvaoApiToken = null,
		SectorAdditionalAirports = []
	};
}
