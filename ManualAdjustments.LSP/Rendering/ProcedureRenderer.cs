using CIFPReader;

using ManualAdjustments.LSP.Types.Semantics;

using SkiaSharp;

namespace ManualAdjustments.LSP.Rendering;

internal static class ProcedureRenderer
{
	private const decimal BUFFER = 0.15m;

	// Colours: C_<colour>
	private readonly static SKColor C_BACKGROUND = new(0, 0, 0);
	private readonly static SKColor C_PRIMARY = new(0xFF, 0x00, 0x80);
	private readonly static SKColor C_SECONDARY = new(0x99, 0x99, 0x99);

	// Fills: P_F_<fill>
	private readonly static SKPaint P_F_PRIMARY = new() { Color = C_PRIMARY, IsStroke = false };
	private readonly static SKPaint P_F_SECONDARY = new() { Color = C_SECONDARY, IsStroke = false };

	// Strokes: P_S_<stroke>
	private readonly static SKPaint P_S_PRIMARY = new() { Color = C_PRIMARY, IsStroke = true, StrokeWidth = 2.5f, IsAntialias = true };
	private readonly static SKPaint P_S_SECONDARY = new() { Color = C_SECONDARY, IsStroke = true, StrokeWidth = 1.5f, IsAntialias = true };

	public static string RenderPngGeosBase64(int width, int height, CIFP cifp, params LspGeo[] geos) => VectorRenderer.RenderBase64(width, height, Draw(width, height, cifp, geos));

	public static string RenderSvgGeosBase64(int width, int height, CIFP cifp, params LspGeo[] geos) => VectorRenderer.RenderSvgBase64(width, height, Draw(width, height, cifp, geos));

	public static SKImage RenderGeos(int width, int height, CIFP cifp, params LspGeo[] geos) => VectorRenderer.Render(width, height, Draw(width, height, cifp, geos));

	private static Action<SKCanvas> Draw(int width, int height, CIFP cifp, LspGeo[] geos) => canvas => {
		canvas.Clear(C_BACKGROUND);
		IDrawableGeo[] drawables = [..
			geos.Where(g => g.Resolve(cifp)).Select(static g => g.Geo)
		];

		// Determine the bounding box.
		decimal minLat = 90, maxLat = -90, minLon = 180, maxLon = -180;

		foreach (ICoordinate coord in drawables.SelectMany(static d => (IEnumerable<ICoordinate>)[
			..d.ReferencePoints,
			..(d is GeoConnector gc ? gc.Points.Select(static p => p.Coordinate).Where(static c => c is not null).Cast<ICoordinate>()
			 : d is GeoSymbol gs && gs.Centerpoint.Coordinate is ICoordinate cpCoord ? [cpCoord]
			 : [])
		]).DistinctBy(static coord => HashCode.Combine(coord.Latitude, coord.Longitude)))
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

		decimal latPad = (maxLat - minLat) * BUFFER,
				lonPad = (maxLon - minLon) * BUFFER;
		minLat -= latPad;
		maxLat += latPad;
		minLon -= lonPad;
		maxLon += lonPad;

		static float mercatorX(decimal longitude) =>
			(float)(longitude + 180) / 360;

		static float mercatorY(decimal latitude) =>
			(float)(latitude + 90) / 180;
			// MathF.Log(MathF.Tan((float)latitude * DEG_TO_RAD) + 1 / MathF.Cos((float)latitude * DEG_TO_RAD));

		float mercatorMinLat = mercatorY(minLat),
			  mercatorMaxLat = mercatorY(maxLat),
			  mercatorMinLon = mercatorX(minLon),
			  mercatorMaxLon = mercatorX(maxLon),
			  scaleDownX = 1f / (mercatorMaxLon - mercatorMinLon),
			  scaleDownY = 1f / (mercatorMaxLat - mercatorMinLat),
			  minScale = MathF.Min(scaleDownX, scaleDownY);

		int scaleUp = Math.Min(width, height),
			padX = (width - scaleUp) / 2,
			padY = (height - scaleUp) / 2;

		float yTop = (minScale / scaleDownY + 1) * 0.5f,
			  xLeft = (1 - (mercatorMaxLon - mercatorMinLon) * minScale) * 0.5f;

		float minX = 1, maxX = 0, minY = 1, maxY = 0;

		SKPoint convert(ICoordinate coord, bool track = true)
		{
			// Scale the coordinate down into a [0,1] mercator space.
			float x = xLeft + ((mercatorX(coord.Longitude) - mercatorMinLon) * minScale);
			float y = yTop - ((mercatorY(coord.Latitude) - mercatorMinLat) * minScale); // Invert y-axis

			if (track)
			{
				minX = MathF.Min(minX, x);
				maxX = MathF.Max(maxX, x);
				minY = MathF.Min(minY, y);
				maxY = MathF.Max(maxY, y);
			}

			return new(
				x * scaleUp + padX,
				y * scaleUp + padY
			);
		}

		float canvasRotation = 0;
		// Try to rotate mag north up if possible.
		if (cifp.Aerodromes.Values.FirstOrDefault(ad =>
									ad.Location.Latitude > minLat && ad.Location.Latitude < maxLat &&
									ad.Location.Longitude < maxLon && ad.Location.Longitude > minLon) is Aerodrome rotCentreAp)
			canvasRotation = (float)rotCentreAp.MagneticVariation;
		else if (cifp.Navaids.Values.SelectMany(static v => v).FirstOrDefault(nav =>
									nav.MagneticVariation is not null &&
									nav.Position.Latitude > minLat && nav.Position.Latitude < maxLat &&
									nav.Position.Longitude < maxLon && nav.Position.Longitude > minLon) is Navaid rotCentreNav)
			canvasRotation = (float)rotCentreNav.MagneticVariation!;

		canvas.RotateDegrees(canvasRotation, width / 2, height / 2);
		SKFont font = SKFontManager.Default.MatchCharacter('K').ToFont(10f);

		// Draw any on-screen navaids.
		foreach (Navaid nav in cifp.Navaids.Values.SelectMany(static v => v).Where(nav =>
									nav is not (NavaidILS or ILS) &&
									nav.Position.Latitude > minLat && nav.Position.Latitude < maxLat &&
									nav.Position.Longitude < maxLon && nav.Position.Longitude > minLon))
		{
			SKPoint location = convert(nav.Position, false);
			canvas.Save();
			canvas.RotateDegrees(-canvasRotation, location.X, location.Y);
			canvas.DrawRect(location.X - 2.5f, location.Y - 2.5f, 5, 5, P_S_SECONDARY);
			canvas.DrawText(nav.Identifier, location with { Y = location.Y - 5 }, SKTextAlign.Center, font, P_F_SECONDARY);
			canvas.Restore();
		}

		// Draw the on-screen airports and their runways.
		foreach (Aerodrome ap in cifp.Aerodromes.Values.Where(ad =>
									ad.Location.Latitude > minLat && ad.Location.Latitude < maxLat &&
									ad.Location.Longitude < maxLon && ad.Location.Longitude > minLon))
		{
			if (cifp.Runways.TryGetValue(ap.Identifier, out var rws))
			{
				// Yay! We found runways! Pair them up and draw them.
				HashSet<string> processed = [];

				foreach (var rw in rws)
				{
					if (processed.Contains(rw.Identifier) || rws.FirstOrDefault(oppo => oppo.Identifier == rw.OppositeIdentifier) is not Runway oppo)
						continue;

					processed.Add(oppo.Identifier);
					canvas.DrawLine(convert(rw.Endpoint, false), convert(oppo.Endpoint, false), P_S_SECONDARY);
				}
			}

			// Draw the airport dot/label itself.
			SKPoint location = convert(ap.Location, false);
			canvas.DrawCircle(location, 5, P_F_SECONDARY);
			canvas.Save();
			canvas.RotateDegrees(-canvasRotation, location.X, location.Y);
			canvas.DrawText(ap.Identifier, location with { Y = location.Y - 10 }, SKTextAlign.Center, font, P_F_SECONDARY);
			canvas.Restore();
		}

		HashSet<NamedCoordinate> namedPoints = [];

		// Draw the actual proc/geo/whatever.
		SKPath path = new();
		foreach (IDrawableGeo geo in drawables)
		{
			bool lastBreak = true;

			foreach (ICoordinate? coordOpt in geo.Draw())
			{
				if (coordOpt is not ICoordinate coord)
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

			namedPoints.UnionWith(geo.ReferencePoints.Where(static c => c is NamedCoordinate).Cast<NamedCoordinate>());
		}

		canvas.DrawPath(path, P_S_PRIMARY);

		// Draw any of the named points.
		foreach (NamedCoordinate nc in namedPoints)
		{
			SKPoint location = convert(nc);
			canvas.Save();
			canvas.RotateDegrees(-canvasRotation, location.X, location.Y);
			canvas.DrawText(nc.Name, location with { Y = location.Y - 10 }, SKTextAlign.Center, font, P_F_PRIMARY);
			canvas.Restore();
			canvas.DrawCircle(location, 3f, P_F_PRIMARY);
		}
	};
}
