using System.Text.Json.Serialization;

namespace ManualAdjustments.LSP.Types;

internal enum SemanticTokenType : uint
{
	/// <summary>For identifiers that declare or reference a namespace, module, or package.</summary>
	Namespace,
	/// <summary>For identifiers that declare or reference a class type.</summary>
	Class,
	/// <summary>For identifiers that declare or reference an enumeration type.</summary>
	Enum,
	/// <summary>For identifiers that declare or reference an interface type.</summary>
	Interface,
	/// <summary>For identifiers that declare or reference a struct type.</summary>
	Struct,
	/// <summary>For identifiers that declare or reference a type parameter.</summary>
	TypeParameter,
	/// <summary>For identifiers that declare or reference a type that is not covered above.</summary>
	Type,
	/// <summary>For identifiers that declare or reference a function or method parameters.</summary>
	Parameter,
	/// <summary>For identifiers that declare or reference a local or global variable.</summary>
	Variable,
	/// <summary>For identifiers that declare or reference a member property, member field, or member variable.</summary>
	Property,
	/// <summary>For identifiers that declare or reference an enumeration property, constant, or member.</summary>
	EnumMember,
	/// <summary>For identifiers that declare or reference decorators and annotations.</summary>
	Decorator,
	/// <summary>For identifiers that declare an event property.</summary>
	Event,
	/// <summary>For identifiers that declare a function.</summary>
	Function,
	/// <summary>For identifiers that declare a member function or method.</summary>
	Method,
	/// <summary>For identifiers that declare a macro.</summary>
	Macro,
	/// <summary>For identifiers that declare a label.</summary>
	Label,
	/// <summary>For tokens that represent a comment.</summary>
	Comment,
	/// <summary>For tokens that represent a string literal.</summary>
	String,
	/// <summary>For tokens that represent a language keyword.</summary>
	Keyword,
	/// <summary>For tokens that represent a number literal.</summary>
	Number,
	/// <summary>For tokens that represent a regular expression literal.</summary>
	Regexp,
	/// <summary>For tokens that represent an operator.</summary>
	Operator
}

internal record SemanticTokensLegend(
	[property: JsonPropertyName("tokenTypes")] string[] TokenTypes,
	[property: JsonPropertyName("tokenModifiers")] string[] TokenModifiers
)
{
	public static SemanticTokensLegend Default { get; } = new(
		[.. Enum.GetNames<SemanticTokenType>().Select(static n => n.ToLowerInvariant())],
		[]
	);
}

internal record SemanticToken(Position Start, uint Length, SemanticTokenType Type)
{
	public SemanticToken(Twe twe, SemanticTokenType type) : this(
		twe.Position.Start,
		(uint)twe.Lexeme.Length,
		type
	) { }
}

static class SemanticTokenSerialiser
{
	public static IEnumerable<uint> Serialise(this IEnumerable<SemanticToken> tokens)
	{
		int lastLine = 0, lastChar = 0;

		foreach (SemanticToken token in tokens.OrderBy(token => token.Start))
		{
			// First two: Deltas for line and char of token start.
			if (token.Start.Line == lastLine)
			{
				yield return 0; // deltaLine
				yield return (uint)(token.Start.Character - lastChar); // deltaChar
			}
			else
			{
				yield return (uint)(token.Start.Line - lastLine); // deltaLine
				yield return (uint)token.Start.Character; // deltaChar
			}

			(lastLine, lastChar) = token.Start;

			// Next: Token length.
			yield return token.Length;

			// Token type.
			yield return (uint)token.Type;

			// Token modifiers.
			yield return 0;
		}
	}
}
