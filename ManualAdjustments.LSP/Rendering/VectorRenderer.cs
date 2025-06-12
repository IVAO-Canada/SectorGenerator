using SkiaSharp;

namespace ManualAdjustments.LSP.Rendering;

internal static class VectorRenderer
{
	public static SKImage Render(int width, int height, Action<SKCanvas> draw)
	{
		SKImageInfo info = new(width, height);

		// Create the surface and call the drawing function.
		using SKSurface surface = SKSurface.Create(info);
#if DEBUG
		DateTimeOffset start = DateTimeOffset.Now;
#endif
		draw(surface.Canvas);
#if DEBUG
		System.Diagnostics.Debug.WriteLine($"Draw call took {(DateTimeOffset.Now - start).TotalMilliseconds:000.00}ms");
#endif

		return surface.Snapshot();
	}

	public static string RenderSvg(int width, int height, Action<SKCanvas> draw)
	{
		// Create the SVG canvas and call the drawing function.
		MemoryStream drawStream = new();
		using (SKCanvas canvas = SKSvgCanvas.Create(SKRect.Create(width, height), new SKManagedWStream(drawStream, false)))
		{
#if DEBUG
			DateTimeOffset start = DateTimeOffset.Now;
#endif
			draw(canvas);
#if DEBUG
			System.Diagnostics.Debug.WriteLine($"Draw call took {(DateTimeOffset.Now - start).TotalMilliseconds:000.00}ms");
#endif
		}

		drawStream.Position = 0;

		// Read it out into something usable.
		using StreamReader reader = new(drawStream);
		string res = reader.ReadToEnd();

		drawStream.Dispose();
		return res;
	}

	public static string RenderBase64(int width, int height, Action<SKCanvas> draw)
	{
		using SKImage image = Render(width, height, draw);
		using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
		string imageData = Convert.ToBase64String(data.Span);

		return $"data:image/png;base64,{imageData}";
	}

	public static string RenderSvgBase64(int width, int height, Action<SKCanvas> draw)
	{
		string svg = RenderSvg(width, height, draw);
		string imageData = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(svg));

		return $"data:image/svg+xml;base64,{imageData}";
	}
}
