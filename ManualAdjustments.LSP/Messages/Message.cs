using ManualAdjustments.LSP.Types;

using System.Text.Json.Serialization;

namespace ManualAdjustments.LSP.Messages;

internal abstract record Message()
{
	[JsonPropertyName("jsonrpc")] public string JsonRpc { get; init; } = "2.0";
}

internal record RequestMessage(
	[property: JsonPropertyName("id")] int Id,
	[property: JsonPropertyName("method")] string Method
) : Message();

internal record RequestMessage<T, U>(
	int Id,
	string Method,
	[property: JsonPropertyName("params")] T Params
) : RequestMessage(Id, Method) where T : IRequestParams<U> where U : ResponseMessage;

internal record ResponseMessage(
	[property: JsonPropertyName("id")] int Id,
	[property: JsonPropertyName("error")] ResponseError? Error
) : Message();

internal record ResponseMessage<T>(
	int Id,
	[property: JsonPropertyName("result")] T Result
) : ResponseMessage(Id, null) where T : IResult;

internal record NotificationMessage(
	[property: JsonPropertyName("id")] int Id,
	[property: JsonPropertyName("method")] string Method
) : Message();

internal record NotificationMessage<T>(
	int Id,
	string Method,
	[property: JsonPropertyName("params")] T Params
) : NotificationMessage(Id, Method) where T : INotificationParams;
