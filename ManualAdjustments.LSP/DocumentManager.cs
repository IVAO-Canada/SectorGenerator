using ManualAdjustments.LSP.Messages.Language;

using System.Diagnostics.CodeAnalysis;

using File = ManualAdjustments.LSP.Types.Semantics.File;

namespace ManualAdjustments.LSP;

internal class DocumentManager
{
	private readonly Dictionary<string, string> _documentText = [];
	private readonly Dictionary<string, ParseResult> _documentParses = [];
	private readonly Dictionary<string, File> _documentTrees = [];

	public void Add(string uri, string text)
	{
		_documentText[uri] = text;
		GenerateDiagnostics(uri);
	}

	public void Update(string uri, string text)
	{
		_documentText[uri] = text;
		_documentParses.Remove(uri);
		_documentTrees.Remove(uri);
		GenerateDiagnostics(uri);
	}

	private static void GenerateDiagnostics(string uri) =>
		_ = PublishDiagnosticsNotificationParams.PublishAsync(uri);

	public string GetText(string uri) => _documentText[uri];
	public bool TryGetText(string uri, [NotNullWhen(true)] out string? text) => _documentText.TryGetValue(uri, out text);

	public ParseResult GetParse(string uri)
	{
		if (_documentParses.TryGetValue(uri, out var cachedResult))
			return cachedResult;

		_documentParses[uri] = Parsing.Parse(GetText(uri));
		return _documentParses[uri];
	}

	public bool TryGetParse(string uri, [NotNullWhen(true)] out ParseResult? parse)
	{
		if (_documentParses.TryGetValue(uri, out parse))
			return true;

		if (!TryGetText(uri, out string? text))
			return false;

		parse = _documentParses[uri] = Parsing.Parse(text);
		return true;
	}

	public File GetTree(string uri)
	{
		if (_documentTrees.TryGetValue(uri, out var cachedResult))
			return cachedResult;

		_documentTrees[uri] = File.Construct(GetParse(uri));
		return _documentTrees[uri];
	}

	public bool TryGetTree(string uri, [NotNullWhen(true)] out File? tree)
	{
		if (_documentTrees.TryGetValue(uri, out tree))
			return true;

		if (!TryGetParse(uri, out ParseResult? parse))
			return false;

		tree = _documentTrees[uri] = File.Construct(parse);
		return true;
	}

	public void Remove(string uri)
	{
		_documentText.Remove(uri);
		_documentParses.Remove(uri);
		_documentTrees.Remove(uri);
	}
}
