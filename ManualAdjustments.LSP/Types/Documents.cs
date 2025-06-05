using System.Text.Json.Serialization;

namespace ManualAdjustments.LSP.Types.Documents;

internal record struct WorkspaceFolder(
	[property: JsonPropertyName("uri")] string Uri,
	[property: JsonPropertyName("name")] string Name
);
