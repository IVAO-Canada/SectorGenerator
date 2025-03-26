using System.Text.Json;
using System.Text.Json.Serialization;

namespace SectorGenerator;

public struct Config
{
	public string OutputFolder { get; set; }

	public string? IvaoApiRefresh { get; set; }

	public Dictionary<string, string[]> SectorAdditionalAirports { get; set; }

	public string? ManualAdjustmentsFolder { get; set; }

	[JsonIgnore]
	public static Config Default => new() {
		OutputFolder = Environment.GetEnvironmentVariable("SECTORFILES_FOLDER") ?? "SectorFiles",
		IvaoApiRefresh = Environment.GetEnvironmentVariable("IVAO_REFRESH") ?? null,
		SectorAdditionalAirports = JsonSerializer.Deserialize<Dictionary<string, string[]>>(Environment.GetEnvironmentVariable("ADDITIONAL_AIRPORTS") ?? "{}") ?? [],
		ManualAdjustmentsFolder = null
	};
}
