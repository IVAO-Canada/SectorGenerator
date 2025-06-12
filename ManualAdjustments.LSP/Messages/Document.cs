using CIFPReader;

using ManualAdjustments.LSP.Messages.Language;
using ManualAdjustments.LSP.Rendering;
using ManualAdjustments.LSP.Types;
using ManualAdjustments.LSP.Types.Documents;
using ManualAdjustments.LSP.Types.Semantics;

using System.Text.Json.Serialization;

using File = ManualAdjustments.LSP.Types.Semantics.File;

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

	public async Task HandleAsync()
	{
		DocumentManager manager = InjectionContext.Shared.Get<DocumentManager>();
		manager.Update(TextDocument.Uri, ContentChanges[^1].Text);

		if (manager.TryGetTree(TextDocument.Uri, out File? file))
		{
			LspGeo[] renderTargets = [.. file.Adjustments.SelectMany(static a => a is GeoDefinition g ? g.Geos : a is ProcedureDefinition p ? p.Geos : [])];
			string svg = ProcedureRenderer.RenderSvgGeosBase64(800, 650, InjectionContext.Shared.Get<CIFP>(), renderTargets);

			await InjectionContext.Shared.Get<Server>().SendNotificationAsync<ImagePreviewNotificationParams>(new(
				ImagePreviewNotificationParams.Method,
				new(svg)
			));
		}
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
