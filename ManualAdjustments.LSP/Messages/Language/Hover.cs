using CIFPReader;

using ManualAdjustments.LSP.Types;
using ManualAdjustments.LSP.Types.Documents;
using ManualAdjustments.LSP.Types.Semantics;

using System.Text.Json.Serialization;

namespace ManualAdjustments.LSP.Messages.Language;

internal record HoverRequestParams(
	[property: JsonPropertyName("textDocument")] TextDocumentIdentifier TextDocument,
	[property: JsonPropertyName("position")] Position Position
) : IRequestParams
{
	public static string Method => "textDocument/hover";

	public Task<ResponseMessage> HandleAsync(int id)
	{
		var file = InjectionContext.Shared.Get<DocumentManager>().GetTree(TextDocument.Uri);

		if (file.Adjustments.FirstOrDefault(adj => adj.Range.CompareTo(Position) is 0) is not ProcedureDefinition proc)
			return Task.FromResult(new ResponseMessage(id, null));

		if (proc.AirportRange.CompareTo(Position) is 0 && InjectionContext.Shared.Get<CIFP>().Aerodromes.TryGetValue(proc.Airport, out Aerodrome? airport))
		{
			string usage = airport.Usage switch {
				Aerodrome.AirportUsage.Military => " (MIL)",
				Aerodrome.AirportUsage.Private => " (PRIVATE)",
				_ => ""
			};

			return Task.FromResult((ResponseMessage)new ResponseMessage<Hover>(
				id,
				new($"{airport.Name}{usage}: [AirNav](https://airnav.com/airport/{airport.Identifier}) [SkyVector](https://skyvector.com/airport/{airport.Identifier})", proc.AirportRange)
			));
		}
		else if (proc.NameRange.CompareTo(Position) is 0 && InjectionContext.Shared.Get<FaaCycleData>().Charts.TryGetValue(proc.Airport, out var apProcs) && apProcs.TryGetValue(proc.Name, out string[]? chartUrls))
		{
			string text =
				"Charts: " +
				string.Join(", ", chartUrls.Select(static (c, idx) => $"[Page {idx + 1}]({c})"));

			return Task.FromResult((ResponseMessage)new ResponseMessage<Hover>(
				id,
				new(text, proc.NameRange)
			));
		}
		else
			return Task.FromResult(new ResponseMessage(id, null));
	}
}

internal record Hover(
	[property: JsonPropertyName("contents")] string Markup,
	[property: JsonPropertyName("range")] Range Range
) : IResult;
