using System.Text.Json.Serialization;

namespace ManualAdjustments.LSP.Types;

internal record struct Color(
	[property: JsonPropertyName("red")] float Red,
	[property: JsonPropertyName("green")] float Green,
	[property: JsonPropertyName("blue")] float Blue,
	[property: JsonPropertyName("alpha")] float Alpha
)
{
	public static Color ParseAurora(string auroraColour)
	{
		auroraColour = auroraColour.Trim();

		if (auroraColour.Length is < 6 or > 9 ||
			(auroraColour.Length % 2 is 1 && (auroraColour[0] is not '#' || !auroraColour[1..].All(char.IsAsciiHexDigit))) ||
			(auroraColour.Length % 2 is 0 && !auroraColour.All(char.IsAsciiHexDigit)))
			throw new ArgumentException("Invalid colour.", nameof(auroraColour));

		static float Fix(string hex) => Convert.ToInt32(hex, 16) / 255f;

		auroraColour = auroraColour.TrimStart('#');
		return new(
			Alpha: Fix(auroraColour[..2]),
			Red: Fix(auroraColour[2..4]),
			Green: Fix(auroraColour[4..6]),
			Blue: Fix(auroraColour[6..])
		);
	}

	public override readonly string ToString() => $"#{(int)(Red * 255):X2}{(int)(Green * 255):X2}{(int)(Blue * 255):X2}{(int)(Alpha * 255):X2}";
	public readonly string ToAuroraString() => $"#{(int)(Alpha * 255):X2}{(int)(Red * 255):X2}{(int)(Green * 255):X2}{(int)(Blue * 255):X2}";
}
