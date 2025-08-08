using System.Collections.Immutable;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace ManualAdjustments;

public static partial class Parsing
{
	private const RegexOptions FLAGS = RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.ExplicitCapture;

	internal static ImmutableDictionary<int, ImmutableHashSet<Twe>> Lex(string input)
	{
		HashSet<Twe> allTerminals = [];

		List<int> lineStarts = [0];

		for (int inputIdx = 0; inputIdx < input.Length; ++inputIdx)
			if (input[inputIdx] is '\r' or '\n')
			{
				if (input.AsSpan()[inputIdx..].StartsWith("\r\n"))
					// \r\n is one line break!
					++inputIdx;

				// Line starts the character _AFTER_ the line break.
				lineStarts.Add(inputIdx + 1);
			}

		Position IdxToPosition(int idx)
		{
			int line;

			for (line = 0; line < lineStarts.Count && idx >= lineStarts[line]; ++line)
				// Loop!
				;

			line = Math.Max(0, line - 1);

			int position = idx - lineStarts[line];
			return new(line, position);
		}

		void AddMatches(Regex regex, Twe.Token token)
		{
			foreach (ValueMatch match in regex.EnumerateMatches(input))
				allTerminals.Add(new(
					token,
					input.Substring(match.Index, match.Length),
					new(IdxToPosition(match.Index), IdxToPosition(match.Index + match.Length))
				));
		}

		/// <summary>Find all of the given literals in the string and add them to the set.</summary>
		void AddSearch(Twe.Token token, params string[] literals)
		{
			foreach (string literal in literals)
			{
				ReadOnlySpan<char> searchInput = input;
				int locatedIndex = -1, sumIndex = 0;

				while ((locatedIndex = searchInput.IndexOf(literal)) >= 0)
				{
					sumIndex += locatedIndex;
					Position start = IdxToPosition(sumIndex);
					Position end = start with { Character = start.Character + literal.Length };
					searchInput = searchInput[(locatedIndex + literal.Length)..];
					sumIndex += literal.Length;
					allTerminals.Add(new(token, literal, new(start, end)));
				}
			}
		}

		AddMatches(NameRegex(), Twe.Token.Name);
		AddMatches(AirportRegex(), Twe.Token.Airport);
		AddMatches(CoordinateRegex(), Twe.Token.Coordinate);
		AddMatches(RadialDistRegex(), Twe.Token.RadialDist);
		AddMatches(ParameterRegex(), Twe.Token.Parameter);
		AddMatches(IndentRegex(), Twe.Token.Indent);

		AddSearch(Twe.Token.AirwayType, "HIGH", "LOW");
		AddSearch(Twe.Token.ProcType, "SID", "STAR", "IAP");
		AddSearch(Twe.Token.SymbolType, "POINT", "CIRCLE", "WAYPOINT", "TRIANGLE", "NUCLEAR", "FLAG", "DIAMOND", "CHEVRON", "BOX", "STAR");
		AddSearch(Twe.Token.ConnectorType, "LINE", "DASH", "ARROW", "ARC", "DASHARC");
		AddSearch(Twe.Token.DefType, "FIX", "AIRWAY", "VFRFIX", "VFRROUTE", "GEO", "PROC");
		AddSearch(Twe.Token.Colon, ":");
		AddSearch(Twe.Token.Delete, "DELETE");

		// Remove any names that start lines. These shouldn't happen and screw with parsing.
		// TODO: This will invariable cause problems with GEO blocks as they start with an INDENT token.
		allTerminals.RemoveWhere(static twe => twe.Type is Twe.Token.Name && twe.Position.Start.Character == 0);

		return allTerminals
			.GroupBy(twe => lineStarts[twe.Position.Start.Line] + twe.Position.Start.Character)
			.ToImmutableDictionary(
				static tweGroup => tweGroup.Key,
				static tweGroup => tweGroup.ToImmutableHashSet()
			);
	}

	[GeneratedRegex(@"(\b[\w/]+(?!\d{0,2}@)|""[^""]+"")", FLAGS)]
	private static partial Regex NameRegex();

	[GeneratedRegex(@"\b[A-Z]{4}\b", FLAGS)]
	private static partial Regex AirportRegex();

	[GeneratedRegex(@"\((?<lat>[+-]?\d+(\.\d+)?)\s*\W\s*(?<lon>[+-]?\d+(\.\d+)?)\)", FLAGS)]
	private static partial Regex CoordinateRegex();

	[GeneratedRegex(@"(?<radial>\d{3})@(?<dist>\d+(\.\d*)?)\b", FLAGS)]
	private static partial Regex RadialDistRegex();

	[GeneratedRegex(@"\((\d+(\.\d+)?|#[\dA-Fa-f]{6}([\dA-Fa-f]{2})?|[NSEW])\)", FLAGS)]
	private static partial Regex ParameterRegex();

	[GeneratedRegex(@"^[^\S\r\n]+(?=\S)", FLAGS)]
	private static partial Regex IndentRegex();
}

/// <summary>Represents a location within a file.</summary>
/// <param name="Line">The zero-based line number within the file.</param>
/// <param name="Character">The zero-based character index within the <paramref name="Line"/>.</param>
public record Position(
	[property: JsonPropertyName("line")] int Line,
	[property: JsonPropertyName("character")] int Character
) : IComparable<Position>
{
	public int CompareTo(Position? other)
	{
		if (other is not Position p)
			throw new ArgumentNullException(nameof(other));

		int lineCmp = Line.CompareTo(p.Line);

		return lineCmp is 0 ? Character.CompareTo(p.Character) : lineCmp;
	}

	public static bool operator <(Position left, Position right) => left.CompareTo(right) < 0;
	public static bool operator >(Position left, Position right) => left.CompareTo(right) > 0;

	public static Position operator +(Position position, int charOffset) => position with {
		Character = position.Character + charOffset
	};
	public static Position operator -(Position position, int charOffset) => position + (-charOffset);

	public override string ToString() => $"{Line}:{Character}";
}

public record Range(
	[property: JsonPropertyName("start")] Position Start,
	[property: JsonPropertyName("end")] Position End
) : IComparable<Range>, IComparable<Position>
{
	public int CompareTo(Range? other)
	{
		if (other is not Range r)
			throw new ArgumentNullException(nameof(other));

		int startCmp = Start.CompareTo(r.Start);

		return startCmp is 0 ? End.CompareTo(r.End) : startCmp;
	}

	public int CompareTo(Position? other)
	{
		if (other is not Position p)
			throw new ArgumentNullException(nameof(other));

		int startCmp = Start.CompareTo(p);
		int endCmp = End.CompareTo(p);

		if (startCmp < 0 && endCmp < 0) return startCmp;
		else if (startCmp > 0 && endCmp > 0) return endCmp;
		// Note: This allows searching inside reversed ranges.
		else return 0;
	}

	public Range ExpandTo(Position inclusion) => this with {
		Start = inclusion < Start ? inclusion : Start,
		End = inclusion > End ? inclusion : End
	};

	public Range ExpandTo(Range inclusion) => ExpandTo(inclusion.Start).ExpandTo(inclusion.End);

	public override string ToString() => $"{Start}..{End}";
}

public record Twe(Twe.Token Type, string Lexeme, Range Position)
{
	public enum Token
	{
		Name,
		Airport,
		Coordinate,
		RadialDist,
		Parameter,
		Indent,
		AirwayType,
		ProcType,
		SymbolType,
		ConnectorType,
		DefType,
		Colon,
		Delete
	}
}
