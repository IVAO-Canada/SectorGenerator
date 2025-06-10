using SkiaSharp;

namespace ManualAdjustments.LSP.Rendering;

internal static class VectorRenderer
{
	public static SKImage Render(int width, int height, Action<SKCanvas> draw)
	{
		SKImageInfo info = new(width, height);

		// Create the surface and call the drawing function.
		using SKSurface surface = SKSurface.Create(info);
		draw(surface.Canvas);

		return surface.Snapshot();
	}

	public static string RenderBase64(int width, int height, Action<SKCanvas> draw)
	{
		using SKImage image = Render(width, height, draw);
		using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
		string imageData = Convert.ToBase64String(data.Span);

		return $"data:image/png;base64,{imageData}";
	}
}
