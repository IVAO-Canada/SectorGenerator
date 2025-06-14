using CIFPReader;

using ManualAdjustments.LSP.Rendering;
using ManualAdjustments.LSP.Types;
using ManualAdjustments.LSP.Types.Documents;
using ManualAdjustments.LSP.Types.Semantics;

using System.Text.Json.Serialization;

namespace ManualAdjustments.LSP.Messages;

internal record SelectionChangedNotificationParams(
	[property: JsonPropertyName("textDocument")] TextDocumentIdentifier TextDocument,
	[property: JsonPropertyName("positions")] Position[] Positions
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

		Adjustment[] adjustments = [.. file.Adjustments.Where(a => Positions.Any(p => a.Range.CompareTo(p) is 0))];
		// Nothing going. Leave it there. Skip and move on.

		static LspGeo fromPoints(Location[] points) => new(
			new GeoConnector.Line([..points.Select(static p => new PossiblyResolvedWaypoint(p.Coordinate, null, null))]),
			points,
			points.Length is 0
			? new(new(0, 0), new(0, 0))
			: new(points.Min(static p => p.Range.Start)!, points.Max(static p => p.Range.End)!)
		);

		static IEnumerable<LspGeo> pullGeos(Adjustment a) => a switch {
			GeoDefinition geo => geo.Geos,
			ProcedureDefinition proc => proc.Geos,
			AirwayDefinition awy => [fromPoints(awy.Points)],
			VfrRouteDefinition vrr => [fromPoints(vrr.Points)],
			_ => []
		};

		geos = [..
			adjustments.Length is 0
			? file.Adjustments.SelectMany(pullGeos)	// Nothing in particular selected. Just render it all!
			: adjustments.SelectMany(pullGeos)		// Collate all the requested GEOs.
		];

		string base64Data = ProcedureRenderer.RenderSvgGeosBase64(800, 650, cifp, geos);

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
