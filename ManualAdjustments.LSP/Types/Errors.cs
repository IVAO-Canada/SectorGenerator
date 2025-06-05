using System.Text.Json.Serialization;

namespace ManualAdjustments.LSP.Types;

internal record ResponseError(
	[property: JsonPropertyName("code")] ErrorCode Code,
	[property: JsonPropertyName("message")] string Message,
	[property: JsonPropertyName("data")] dynamic? Data
);

internal enum ErrorCode
{
	ParseError = -32700,
	InvalidRequest = -32600,
	MethodNotFound = -32601,
	InvalidParams = -32602,
	InternalError = -32603,

	///<summary>
	/// Error code indicating that a server received a notification or
	/// request before the server received the `initialize` request.
	///</summary>
	ServerNotInitialized = -32002,
	UnknownErrorCode = -32001,

	///<summary>
	/// This is the end range of JSON-RPC reserved error codes.
	/// It doesn't denote a real error code.
	///
	/// @since 3.16.0
	///</summary>
	jsonrpcReservedErrorRangeEnd = -32000,

	///<summary>
	/// A request failed but it was syntactically correct, e.g the
	/// method name was known and the parameters were valid. The error
	/// message should contain human readable information about why
	/// the request failed.
	///
	/// @since 3.17.0
	///</summary>
	RequestFailed = -32803,

	///<summary>
	/// The server cancelled the request. This error code should
	/// only be used for requests that explicitly support being
	/// server cancellable.
	///
	/// @since 3.17.0
	///</summary>
	ServerCancelled = -32802,

	///<summary>
	/// The server detected that the content of a document got
	/// modified outside normal conditions. A server should
	/// NOT send this error code if it detects a content change
	/// in its unprocessed messages. The result even computed
	/// on an older state might still be useful for the client.
	///
	/// If a client decides that a result is not of any use anymore
	/// the client should cancel the request.
	///
	ContentModified = -32801,

	///<summary>
	/// The client has canceled a request and a server has detected
	/// the cancel.
	///</summary>
	RequestCancelled = -32800,
}
