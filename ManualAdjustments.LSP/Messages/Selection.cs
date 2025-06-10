using CIFPReader;

using ManualAdjustments.LSP.Rendering;
using ManualAdjustments.LSP.Types;
using ManualAdjustments.LSP.Types.Documents;
using ManualAdjustments.LSP.Types.Semantics;

using SkiaSharp;

using System.Reflection.Metadata;
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

		if (file.Adjustments.FirstOrDefault(a => a.Range.CompareTo(Position) is 0) is not Adjustment adjustment)
			// Nothing going. Leave it there.
			return;

		if (adjustment is GeoDefinition geoDef)
		{
			// A geo! Now we're talking. :)
			CIFP cifp = InjectionContext.Shared.Get<CIFP>();

			foreach (var geo in geoDef.Geos)
				geo.Resolve(cifp);

			string base64Data = ProcedureRenderer.RenderGeosBase64(800, 600, geoDef.Geos);

			await server.SendNotificationAsync(new NotificationMessage<ImagePreviewNotificationParams>(
				ImagePreviewNotificationParams.Method,
				new(base64Data)
			));
		}
		else if (adjustment is ProcedureDefinition procDef)
		{
			// A geo! Now we're talking. :)
			CIFP cifp = InjectionContext.Shared.Get<CIFP>();

			foreach (var geo in procDef.Geos)
				geo.Resolve(cifp);

			string base64Data = ProcedureRenderer.RenderGeosBase64(800, 600, procDef.Geos);

			await server.SendNotificationAsync(new NotificationMessage<ImagePreviewNotificationParams>(
				ImagePreviewNotificationParams.Method,
				new(base64Data)
			));
		}
	}
}

internal record ImagePreviewNotificationParams(
	[property: JsonPropertyName("image")] string Image
) : INotificationParams
{
	public static string Method => "$/imagePreview";

	public Task HandleAsync() => throw new NotImplementedException();
}
