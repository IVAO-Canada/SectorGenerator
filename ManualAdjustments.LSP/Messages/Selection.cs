using CIFPReader;

using ManualAdjustments.LSP.Rendering;
using ManualAdjustments.LSP.Types;
using ManualAdjustments.LSP.Types.Documents;
using ManualAdjustments.LSP.Types.Semantics;

using System.Text.Json.Serialization;

namespace ManualAdjustments.LSP.Messages;

internal record SelectionChangedNotificationParams(
	[property: JsonPropertyName("textDocument")] TextDocumentIdentifier TextDocument,
	[property: JsonPropertyName("position")] Position Position
) : INotificationParams
{
	public static string Method => "$/selectionChanged";

	public async Task HandleAsync()
	{
		if (!InjectionContext.Shared.Get<DocumentManager>().TryGetTree(TextDocument.Uri, out var file))
			// Something odd is afoot. Run away!
			return;

		Server server = InjectionContext.Shared.Get<Server>();
		CIFP cifp = InjectionContext.Shared.Get<CIFP>();
		LspGeo[]? geos = null;

		if (file.Adjustments.FirstOrDefault(a => a.Range.CompareTo(Position) is 0) is not Adjustment adjustment) { }
		// Nothing going. Leave it there. Skip and move on.

		else if (adjustment is GeoDefinition geoDef)
			// A geo! Now we're talking. :)
			geos = geoDef.Geos;
		else if (adjustment is ProcedureDefinition procDef)
			// A geo! Now we're talking. :)
			geos = procDef.Geos;

		if (geos is null)
		{
			// Nothing in particular selected. Just render it all!
			LspGeo[] renderTargets = [.. file.Adjustments.SelectMany(static a => a is GeoDefinition g ? g.Geos : a is ProcedureDefinition p ? p.Geos : [])];
			geos = renderTargets;
		}

		string base64Data = ProcedureRenderer.RenderSvgGeosBase64(800, 600, cifp, geos);

		await server.SendNotificationAsync<ImagePreviewNotificationParams>(new(
			ImagePreviewNotificationParams.Method,
			new(base64Data)
		));
	}
}

internal record ImagePreviewNotificationParams(
	[property: JsonPropertyName("image")] string Image
) : INotificationParams
{
	public static string Method => "$/imagePreview";

	public Task HandleAsync() => throw new NotImplementedException();
}
