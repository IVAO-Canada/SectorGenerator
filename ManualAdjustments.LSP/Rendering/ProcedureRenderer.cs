using CIFPReader;

using ManualAdjustments.LSP.Types.Semantics;

using SkiaSharp;

namespace ManualAdjustments.LSP.Rendering;

internal static class ProcedureRenderer
{
	private readonly static SKColor BACKGROUND = new(0, 0, 0);
	private readonly static SKColor PRIMARY = new(0xFF, 0xFF, 0xFF);

	public static string RenderGeosBase64(int width, int height, params LspGeo[] geos) => VectorRenderer.RenderBase64(width, height, Draw(width, height, geos));

	public static SKImage RenderGeos(int width, int height, params LspGeo[] geos) => VectorRenderer.Render(width, height, Draw(width, height, geos));

	private static Action<SKCanvas> Draw(int width, int height, LspGeo[] geos) => canvas => {
		canvas.Clear(BACKGROUND);
		IDrawableGeo[] drawables = [..
			geos.Select(static g => g.Geo)
		];

		// Determine the bounding box.
		decimal minLat = 90, maxLat = -90, minLon = 180, maxLon = -180;

		foreach (Coordinate coord in drawables.SelectMany(static d => d.ReferencePoints))
		{
			if (coord.Latitude < minLat)
				minLat = coord.Latitude;
			if (coord.Latitude > maxLat)
				maxLat = coord.Latitude;

			if (coord.Longitude < minLon)
				minLon = coord.Longitude;
			if (coord.Longitude > maxLon)
				maxLon = coord.Longitude;
		}

		minLat -= 0.1m;
		maxLat += 0.1m;
		minLon -= 0.1m;
		maxLon += 0.1m;

		// Get a naïve ratio.
		decimal latHeight = maxLat - minLat, lonWidth = maxLon - minLon;

		SKPoint convert(Coordinate coord) => new(
			(float)((coord.Longitude - minLon) / lonWidth * width),
			(float)((1 - (coord.Latitude - minLat) / latHeight) * height)
		);

		SKPath path = new();
		foreach (IDrawableGeo geo in drawables)
		{
			bool lastBreak = true;

			foreach (Coordinate? coordOpt in geo.Draw().Select(static c => c?.GetCoordinate()))
			{
				if (coordOpt is not Coordinate coord)
				{
					lastBreak = true;
					continue;
				}

				if (lastBreak)
					path.MoveTo(convert(coord));
				else
					path.LineTo(convert(coord));

				lastBreak = false;
			}
		}

		canvas.DrawPath(path, new() { Color = PRIMARY, IsStroke = true, StrokeWidth = 1f });
	};
}
