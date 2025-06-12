using System.Text.Json.Serialization;

namespace ManualAdjustments.LSP.Types;

internal record Diagnostic(
	[property: JsonPropertyName("range")] Range Range,
	[property: JsonPropertyName("severity")] DiagnosticSeverity Severity,
	[property: JsonPropertyName("code")] string Code,
	[property: JsonPropertyName("codeDescription")] string CodeDescription,
	[property: JsonPropertyName("source")] string Source,
	[property: JsonPropertyName("message")] string Message,
	[property: JsonPropertyName("tags")] DiagnosticTag[] Tags
)
{
	public static Diagnostic Create(Range range, DiagnosticSeverity severity, string code, string codeDescription, string message) => new(
		range,
		severity,
		code,
		codeDescription,
		"auroraMaf",
		message,
		[]
	);
}

public enum DiagnosticSeverity
{
	Error = 1,
	Warning = 2,
	Information = 3,
	Hint = 4
}

public enum DiagnosticTag
{
	Unnecessary = 1,
	Deprecated = 2
}
