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
) : IRequestParams
{
	public static string Method { get; } = "initialize";

	static InitializeRequestParams() =>
		// Trigger the others to register.
		OtherLifecycleMethods.Register();

	public Task<ResponseMessage> HandleAsync(int id) => Task.FromResult((ResponseMessage)new ResponseMessage<InitializeResult>(
		id,
		InitializeResult.Default
	));

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
			["textDocumentSync"] = 1, // Full synchronisation.
			["colorProvider"] = true, // Colours offered.
			["hoverProvider"] = true, // Some things can get tooltips on hover.
		},
		new("Manual Adjustment File LSP Server", null)
	);
}

internal static class OtherLifecycleMethods
{
	public static void Register()
	{
		InjectionContext.Shared.AddHandler("shutdown", (RequestMessage req) => Task.FromResult(new ResponseMessage(req.Id, null)));
		InjectionContext.Shared.AddHandler("exit", (NotificationMessage note) => {
			InjectionContext.Shared.Get<Server>().Dispose();
			return Task.CompletedTask;
		});
	}
}
