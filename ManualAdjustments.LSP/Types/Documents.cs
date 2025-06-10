using System.Text.Json.Serialization;

namespace ManualAdjustments.LSP.Types.Documents;

internal record struct WorkspaceFolder(
	[property: JsonPropertyName("uri")] string Uri,
	[property: JsonPropertyName("name")] string Name
);

internal record TextDocumentItem(
	[property: JsonPropertyName("uri")] string Uri,
	[property: JsonPropertyName("languageId")] string LanguageId,
	[property: JsonPropertyName("version")] int Version,
	[property: JsonPropertyName("text")] string Text
);

internal record TextDocumentIdentifier(
	[property: JsonPropertyName("uri")] string Uri
);

internal record VersionedTextDocumentIdentifier(
	string Uri,
	[property: JsonPropertyName("version")] int Version
) : TextDocumentIdentifier(Uri);

internal record TextDocumentContentChangeEvent(
	[property: JsonPropertyName("text")] string Text
);

internal record TextEdit(
	[property: JsonPropertyName("range")] Range Range,
	[property: JsonPropertyName("newText")] string NewText
);

internal record TextDocumentPositionParams(
	[property: JsonPropertyName("textDocument")] TextDocumentIdentifier TextDocument,
	[property: JsonPropertyName("position")] Position Position
);
