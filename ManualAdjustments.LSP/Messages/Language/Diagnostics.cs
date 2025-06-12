using ManualAdjustments.LSP.Types;

using System.Text.Json.Serialization;

namespace ManualAdjustments.LSP.Messages.Language;

internal record PublishDiagnosticsNotificationParams(
	[property: JsonPropertyName("uri")] string Uri,
	[property: JsonPropertyName("diagnostics")] Diagnostic[] Diagnostics
) : INotificationParams
{
	public static string Method => "textDocument/publishDiagnostics";

	public Task HandleAsync() => throw new NotImplementedException();

	public static async Task PublishAsync(string uri)
	{
		if (!InjectionContext.Shared.Get<DocumentManager>().TryGetParse(uri, out ParseResult? parse))
		{
			// Whole document is screwed. Just make it all one big error.
			if (!InjectionContext.Shared.Get<DocumentManager>().TryGetText(uri, out string? docText))
				// Couldn't figure it out at all! Something isn't right… Was the document closed?
				return;

			await InjectionContext.Shared.Get<Server>().SendNotificationAsync(new NotificationMessage<PublishDiagnosticsNotificationParams>(Method, new(
				uri,
				[Diagnostic.Create(
					new(new(0, 0), new(docText.Count(static c => c is '\n'), docText.Split('\n')[^1].Length - 1)),
					DiagnosticSeverity.Error,
					"MAF-NO-PARSE",
					"Unparsable file",
					"The given file isn't valid at all. Are you sure you've got the right file open?"
				)]
			)));
			return;
		}

		HashSet<Diagnostic> diagnostics = [];
		Queue<ParseResult> frontier = new([parse]);

		// File was parsed. Iterate through and return a generic error for each unparseable range.
		// TODO: Add more specific errors.
		while (frontier.TryDequeue(out ParseResult? res))
			foreach (ParseResult child in res.Children)
			{
				if (child.GetType().IsGenericType)
					frontier.Enqueue(child);
				else
					diagnostics.Add(Diagnostic.Create(
						child.Range,
						DiagnosticSeverity.Error,
						"MAF-GENERIC",
						"Generic error",
						"This section is invalid and no more specific error was found. Please send this file to Wes to improve the tool!"
					));
			}

		await InjectionContext.Shared.Get<Server>().SendNotificationAsync(new NotificationMessage<PublishDiagnosticsNotificationParams>(
			Method,
			new(uri, [..diagnostics])
		));
	}
}
