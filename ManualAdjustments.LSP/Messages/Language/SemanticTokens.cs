using ManualAdjustments.LSP.Types;
using ManualAdjustments.LSP.Types.Documents;

using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ManualAdjustments.LSP.Messages.Language;

internal record SemanticTokensRequestParams(
	[property: JsonPropertyName("textDocument")] TextDocumentIdentifier TextDocument
) : IRequestParams
{
	public static string Method => "textDocument/semanticTokens/full";

	public Task<ResponseMessage> HandleAsync(int id)
	{
		if (!InjectionContext.Shared.Get<DocumentManager>().TryGetParse(TextDocument.Uri, out ParseResult? file))
			return Task.FromResult<ResponseMessage>(new(
				id,
				new(ErrorCode.InvalidRequest, "Cannot get tokens for an unopened document", null)
			));

		List<SemanticToken> tokens = [];

		foreach (ParseResult adjustment in file.Children)
			tokens.AddRange(GetTokens(adjustment));

		return Task.FromResult<ResponseMessage>(new ResponseMessage<SemanticTokensResult>(
			id,
			new([.. tokens.Serialise()])
		));
	}

	private static IEnumerable<SemanticToken> GetTokens(ParseResult adjustment)
	{
		#region Top level adjustments
		if (adjustment is ParseResult<AddFix> fixDef)
		{
			yield return new(fixDef.Literals[0], SemanticTokenType.Keyword);
			yield return new(fixDef.Literals[1], SemanticTokenType.Parameter);
			yield return new(fixDef.Literals[2], SemanticTokenType.Operator);

			foreach (SemanticToken token in fixDef.Children.SelectMany(GetTokens))
				yield return token;
		}
		else if (adjustment is ParseResult<RemoveFix> fixDel)
		{
			yield return new(fixDel.Literals[0], SemanticTokenType.Keyword);
			yield return new(fixDel.Literals[1], SemanticTokenType.Parameter);
			yield return new(fixDel.Literals[2], SemanticTokenType.Operator);
			yield return new(fixDel.Literals[3], SemanticTokenType.Keyword);
		}
		else if (adjustment is ParseResult<AddAirway> awyDef)
		{
			yield return new(awyDef.Literals[0], SemanticTokenType.Keyword);
			yield return new(awyDef.Literals[1], SemanticTokenType.Keyword);
			yield return new(awyDef.Literals[2], SemanticTokenType.Parameter);
			yield return new(awyDef.Literals[3], SemanticTokenType.Operator);

			foreach (SemanticToken token in awyDef.Children.SelectMany(GetTokens))
				yield return token;
		}
		else if (adjustment is ParseResult<AddVfrFix> vfrFixDef)
		{
			yield return new(vfrFixDef.Literals[0], SemanticTokenType.Keyword);
			yield return new(vfrFixDef.Literals[1], SemanticTokenType.Parameter);
			yield return new(vfrFixDef.Literals[2], SemanticTokenType.Operator);

			foreach (SemanticToken token in vfrFixDef.Children.SelectMany(GetTokens))
				yield return token;
		}
		else if (adjustment is ParseResult<AddVfrRoute> vfrRteDef)
		{
			yield return new(vfrRteDef.Literals[0], SemanticTokenType.Keyword);
			yield return new(vfrRteDef.Literals[1], SemanticTokenType.Parameter);
			yield return new(vfrRteDef.Literals[2], SemanticTokenType.Operator);

			foreach (SemanticToken token in GetTokens(vfrRteDef.Children.Single()))
				yield return token;
		}
		else if (adjustment is ParseResult<AddGeo> geoDef)
		{
			yield return new(geoDef.Literals[0], SemanticTokenType.Keyword);
			yield return new(geoDef.Literals[1], SemanticTokenType.Parameter);

			if (geoDef.Literals.Length > 3)
				yield return new(
					geoDef.Literals[2].Position.Start + 1,
					(uint)(geoDef.Literals[2].Lexeme.Length - 2),
					SemanticTokenType.String
				);

			yield return new(geoDef.Literals[^1], SemanticTokenType.Operator);

			foreach (SemanticToken token in GetTokens(geoDef.Children.Single()))
				yield return token;
		}
		else if (adjustment is ParseResult<AddProcedure> procDef)
		{
			yield return new(procDef.Literals[0], SemanticTokenType.Keyword);
			yield return new(procDef.Literals[1], SemanticTokenType.Parameter);
			yield return new(procDef.Literals[2], SemanticTokenType.Keyword);
			yield return new(procDef.Literals[3], SemanticTokenType.Parameter);
			yield return new(procDef.Literals[4], SemanticTokenType.Operator);

			if (procDef.Literals.Length > 5)
				yield return new(procDef.Literals[5], SemanticTokenType.Keyword);

			foreach (SemanticToken token in GetTokens(procDef.Children.Single()))
				yield return token;
		}
		#endregion
		#region Common sub-results
		else if (adjustment is ParseResult<PossiblyResolvedWaypoint[]> locList)
			foreach (SemanticToken innerToken in locList.Children.SelectMany(GetTokens))
				yield return innerToken;
		else if (adjustment is ParseResult<PossiblyResolvedWaypoint> loc)
		{
			if (loc.Literals.Length is 1)
				// Single section; either a predefined name or a coordinate.
				yield return new(loc.Literals[0], SemanticTokenType.Label);
			else if (loc.Literals.Length is 2 && loc.Literals[1].Type is Twe.Token.Coordinate)
			{
				// Inline definition.
				yield return new(loc.Literals[0], SemanticTokenType.Parameter);
				yield return new(loc.Literals[1], SemanticTokenType.Label);
			}
			else if (loc.Literals.Length is 2 && loc.Literals[1].Type is Twe.Token.RadialDist)
			{
				// Radial/distance offset from known coordinate.
				yield return new(loc.Literals[0], SemanticTokenType.Label);
				yield return new(loc.Literals[1], SemanticTokenType.Number);
			}
		}
		else if (adjustment is ParseResult<IDrawableGeo[]> geoList)
			foreach (SemanticToken innerToken in geoList.Children.SelectMany(GetTokens))
				yield return innerToken;
		else if (adjustment is ParseResult<IDrawableGeo> geo)
		{
			// Connector/symbol type.
			yield return new(geo.Literals[0], SemanticTokenType.Keyword);

			if (geo.Literals.Length > 1)
				// Parameter.
				yield return new(geo.Literals[1], SemanticTokenType.Parameter);

			foreach (SemanticToken innerToken in geo.Children.SelectMany(GetTokens))
				yield return innerToken;
		}
		#endregion
		else throw new ArgumentException("Unknown adjustment type.", nameof(adjustment));
	}
}

internal record SemanticTokensResult(
	[property: JsonPropertyName("data")] uint[] Data
) : IResult;
