using ManualAdjustments.LSP.Types;
using ManualAdjustments.LSP.Types.Documents;

using System.Text.Json.Serialization;

namespace ManualAdjustments.LSP.Messages;

internal record DidOpenTextDocumentNotificationParams(
	[property: JsonPropertyName("textDocument")] TextDocumentItem TextDocument
) : INotificationParams
{
	public static string Method => "textDocument/didOpen";

	public Task HandleAsync()
	{
		InjectionContext.Shared.Get<DocumentManager>().Add(TextDocument.Uri, TextDocument.Text);
		return Task.CompletedTask;
	}
}

internal record DidChangeTextDocumentNotificationParams(
	[property: JsonPropertyName("textDocument")] VersionedTextDocumentIdentifier TextDocument,
	[property: JsonPropertyName("contentChanges")] TextDocumentContentChangeEvent[] ContentChanges
) : INotificationParams
{
	public static string Method => "textDocument/didChange";

	public Task HandleAsync()
	{
		InjectionContext.Shared.Get<DocumentManager>().Update(TextDocument.Uri, ContentChanges[^1].Text);
		return Task.CompletedTask;
	}
}

internal record DidCloseTextDocumentNotificationParams(
	[property: JsonPropertyName("textDocument")] TextDocumentIdentifier TextDocument
) : INotificationParams
{
	public static string Method => "textDocument/didClose";

	public Task HandleAsync()
	{
		InjectionContext.Shared.Get<DocumentManager>().Remove(TextDocument.Uri);
		return Task.CompletedTask;
	}
}
