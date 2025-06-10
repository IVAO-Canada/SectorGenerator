using ManualAdjustments.LSP.Types;
using ManualAdjustments.LSP.Types.Documents;

using System.Text.Json.Serialization;

namespace ManualAdjustments.LSP.Messages.Language;

internal record DocumentColorRequestParams(
	[property: JsonPropertyName("textDocument")] TextDocumentIdentifier TextDocument
) : IRequestParams
{
	public static string Method => "textDocument/documentColor";

	public Task<ResponseMessage> HandleAsync(int id)
	{
		ParseResult parse = InjectionContext.Shared.Get<DocumentManager>().GetParse(TextDocument.Uri);

		var colours = parse.Children.SelectMany(static parse => {
			if (parse is not ParseResult<AddGeo> geoParse)
				return (ColorInformation[])[];

			if (geoParse.Children[0] is ParseResult<string> colourParse)
				return [
					new(colourParse.Range, Color.ParseAurora(colourParse.Result))
				];
			else
				return [];
		});

		return Task.FromResult((ResponseMessage)new ResponseMessage<ColorInformationSet>(
			id,
			new([.. colours])
		));
	}
}

internal record ColorPresentationRequestParams(
	[property: JsonPropertyName("textDocument")] TextDocumentIdentifier TextDocument,
	[property: JsonPropertyName("color")] Color Colour,
	[property: JsonPropertyName("range")] Range Range
) : IRequestParams
{
	public static string Method => "textDocument/colorPresentation";

	public Task<ResponseMessage> HandleAsync(int id)
	{
		string auroraCol = Colour.ToAuroraString();
		string replacement = auroraCol is "#FF999999" ? "" : $"({auroraCol})";

		return Task.FromResult((ResponseMessage)new ResponseMessage<ColorPresentationSet>(
			id,
			new([
				new(Colour.ToString(), new(Range, replacement))
			])
		));
	}
}

internal record ColorInformation(
	[property: JsonPropertyName("range")] Range Range,
	[property: JsonPropertyName("color")] Color Color
) : IResult;

[JsonConverter(typeof(ArrayResultJsonConverter<ColorInformationSet, ColorInformation>))]
internal record ColorInformationSet(ColorInformation[] Items) : IArrayResult<ColorInformation>
{
	public ColorInformationSet() : this([]) { }
}

internal record ColorPresentation(
	[property: JsonPropertyName("label")] string Label,
	[property: JsonPropertyName("textEdit")] TextEdit TextEdit
) : IResult;

[JsonConverter(typeof(ArrayResultJsonConverter<ColorPresentationSet, ColorPresentation>))]
internal record ColorPresentationSet(ColorPresentation[] Items) : IArrayResult<ColorPresentation>
{
	public ColorPresentationSet() : this([]) { }
}
