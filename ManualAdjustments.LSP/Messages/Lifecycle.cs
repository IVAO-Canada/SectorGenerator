using ManualAdjustments.LSP.Types;
using ManualAdjustments.LSP.Types.Documents;

using System.Text.Json.Serialization;

namespace ManualAdjustments.LSP.Messages.Lifecycle;

internal record InitializeRequestParams(
	[property: JsonPropertyName("processId")] int? ProcessId,
	[property: JsonPropertyName("clientInfo")] InitializeRequestParams.ClientInfoStruct? ClientInfo,
	[property: JsonPropertyName("locale")] string? Locale,
	[property: JsonPropertyName("capabilities")] Dictionary<string, dynamic> Capabilities,
	[property: JsonPropertyName("workspaceFolders")] WorkspaceFolder[]? WorkspaceFolders
) : IRequestParams<ResponseMessage<InitializeResult>>
{
	public static string Method { get; } = "initialize";

	public Task<ResponseMessage<InitializeResult>> HandleAsync() => throw new NotImplementedException();

	internal record struct ClientInfoStruct(string Name, string? Version);
}

internal record InitializeResult(
	[property: JsonPropertyName("capabilities")] Dictionary<string, dynamic> Capabilities,
	[property: JsonPropertyName("serverInfo")] InitializeRequestParams.ClientInfoStruct ServerInfo
) : IResult
{
	public static InitializeResult Default => new(
		new() {
			// TODO: Add capabilities as required.
		},
		new("Manual Adjustment File LSP Server", null)
	);
}
