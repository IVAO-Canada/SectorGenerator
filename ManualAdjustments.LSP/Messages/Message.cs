using ManualAdjustments.LSP.Types;

using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace ManualAdjustments.LSP.Messages;

internal abstract record Message(
	[property: JsonPropertyName("jsonrpc")] string JsonRpc
);

internal record RequestMessage(
	string JsonRpc,
	[property: JsonPropertyName("id")] int Id,
	[property: JsonPropertyName("method")] string Method
) : Message(JsonRpc);

internal record RequestMessage<T, U>(
	string JsonRpc,
	int Id,
	string Method,
	[property: JsonPropertyName("params")] T Params
) : RequestMessage(JsonRpc, Id, Method) where T : IRequestParams<U> where U : ResponseMessage;

internal record ResponseMessage(
	string JsonRpc,
	[property: JsonPropertyName("id")] int Id,
	[property: JsonPropertyName("error")] ResponseError? Error
) : Message(JsonRpc);

internal record ResponseMessage<T>(
	string JsonRpc,
	int Id,
	[property: JsonPropertyName("result")] T Result
) : ResponseMessage(JsonRpc, Id, null) where T : IResult;

internal record NotificationMessage(
	string JsonRpc,
	[property: JsonPropertyName("id")] int Id,
	[property: JsonPropertyName("method")] string Method
) : Message(JsonRpc);

internal record NotificationMessage<T>(
	string JsonRpc,
	int Id,
	string Method,
	[property: JsonPropertyName("params")] T Params
) : NotificationMessage(JsonRpc, Id, Method) where T : INotificationParams;
