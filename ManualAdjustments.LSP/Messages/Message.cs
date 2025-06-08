using ManualAdjustments.LSP.Types;

using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace ManualAdjustments.LSP.Messages;

internal abstract record Message()
{
	[JsonPropertyName("jsonrpc")] public string JsonRpc { get; init; } = "2.0";
}

internal record RequestMessage(
	[property: JsonPropertyName("id")] int Id,
	[property: JsonPropertyName("method")] string Method
) : Message()
{
	[JsonIgnore]
	public virtual Type? ParameterType => null;
}

internal sealed record RequestMessage<T>(
	int Id,
	string Method,
	[property: JsonPropertyName("params")] T Params
) : RequestMessage(Id, Method) where T : IRequestParams
{
	[JsonIgnore]
	public override Type? ParameterType => typeof(T);
}

internal record ResponseMessage(
	[property: JsonPropertyName("id")] int Id,
	[property: JsonPropertyName("error")] ResponseError? Error
) : Message();

internal sealed record ResponseMessage<T>(
	int Id,
	[property: JsonPropertyName("result")] T Result
) : ResponseMessage(Id, null) where T : IResult;

internal record NotificationMessage(
	[property: JsonPropertyName("method")] string Method
) : Message();

internal sealed record NotificationMessage<T>(
	string Method,
	[property: JsonPropertyName("params")] T Params
) : NotificationMessage(Method) where T : INotificationParams;

[JsonConverter(typeof(Message))]
internal class MessageJsonConverter(ImmutableDictionary<string, Type> _paramTypes) : JsonConverter<Message>
{
	public override Message? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType is not JsonTokenType.StartObject)
			throw new JsonException();

		int? optId = null;
		string? optMethod = null;
		JsonObject? optParams = null;
		JsonNode? optResult = null;
		ResponseError? optError = null;

		while (reader.Read() && reader.TokenType is JsonTokenType.PropertyName)
		{
			switch (reader.GetString())
			{
				case "jsonrpc":
					if (!reader.Read() || reader.TokenType is not JsonTokenType.String || reader.GetString() is not "2.0")
						throw new JsonException();
					break;

				case "id":
					if (!reader.Read())
						throw new JsonException();

					if (reader.TokenType is JsonTokenType.Number)
						optId = reader.GetInt32();
					else if (reader.TokenType is JsonTokenType.String)
						optId = int.Parse(reader.GetString()!);
					else
						throw new JsonException();
					break;

				case "method":
					if (!reader.Read() || reader.TokenType is not JsonTokenType.String)
						throw new JsonException();

					optMethod = reader.GetString();
					break;

				case "params":
					if (!reader.Read() || reader.TokenType is not JsonTokenType.StartObject)
						throw new JsonException();

					optParams = JsonSerializer.Deserialize<JsonObject>(ref reader, options) ?? throw new JsonException();
					break;

				case "result":
					if (!reader.Read())
						throw new JsonException();

					optResult = JsonSerializer.Deserialize<JsonNode>(ref reader, options) ?? throw new JsonException();
					break;

				case "error":
					if (!reader.Read() || reader.TokenType is not JsonTokenType.StartObject)
						throw new JsonException();

					optError = JsonSerializer.Deserialize<ResponseError>(ref reader, options) ?? throw new JsonException();
					break;

				default:
					// Invalid property.
					throw new JsonException();
			}
		}

		if (reader.TokenType is not JsonTokenType.EndObject)
			throw new JsonException();

		// Check property combinations
		if (optId is int reqId && optMethod is string reqMethod && optResult is null && optError is null)
		{
			// Request message.
			if (optParams is JsonObject paramObj && _paramTypes.TryGetValue(reqMethod, out Type? paramType))
			{
				var param = paramObj.Deserialize(paramType, options) ?? throw new JsonException();

				return (RequestMessage)Activator.CreateInstance(
					typeof(RequestMessage<>).MakeGenericType(paramType),
					reqId,
					reqMethod,
					param
				)!;
			}
			else
				return new RequestMessage(reqId, reqMethod);
		}
		else if (optId is int rspId && optMethod is null && (optResult is null ^ optError is null))
		{
			// Response message.
			if (optResult is null)
				return new ResponseMessage(rspId, optError);
			else
				// Not really sure how to figure out what to deserialise it to…
				throw new NotImplementedException();
		}
		else if (optId is null && optMethod is string noteMethod && optResult is null && optError is null)
		{
			// Notification message.
			if (optParams is JsonObject paramObj && _paramTypes.TryGetValue(noteMethod, out Type? paramType))
			{
				var param = paramObj.Deserialize(paramType, options) ?? throw new JsonException();

				return (NotificationMessage)Activator.CreateInstance(
					typeof(NotificationMessage<>).MakeGenericType(paramType),
					noteMethod,
					param
				)!;
			}
			else
				return new NotificationMessage(noteMethod);
		}
		else
			throw new JsonException();
	}

	public override void Write(Utf8JsonWriter writer, Message value, JsonSerializerOptions options)
	{
		Type valueType = value.GetType();
		if (valueType.IsGenericType)
		{
			Type genericParam = valueType.GetGenericArguments()[0];
			valueType = valueType.GetGenericTypeDefinition();

			Type targetType;

			if (valueType == typeof(RequestMessage<>))
				targetType = typeof(GenericRequestMessageJsonConverter<>).MakeGenericType(genericParam);
			else if (valueType == typeof(ResponseMessage<>))
				targetType = typeof(GenericResponseMessageJsonConverter<>).MakeGenericType(genericParam);
			else if (valueType == typeof(NotificationMessage<>))
				targetType = typeof(GenericNotificationMessageJsonConverter<>).MakeGenericType(genericParam);
			else throw new ArgumentException("Unknown generic message type.", nameof(value));

			object converter = Activator.CreateInstance(targetType)!;
			targetType.GetMethod("Write")!.Invoke(converter, [writer, value, options]);
		}
		else
		{
			writer.WriteStartObject();
			writer.WriteString("jsonrpc", "2.0");

			if (value switch { RequestMessage req => (int?)req.Id, ResponseMessage rsp => rsp.Id, _ => null } is int id)
				writer.WriteNumber("id", id);

			if (value switch { RequestMessage req => req.Method, NotificationMessage not => not.Method, _ => null } is string method)
				writer.WriteString("method", method);

			if (value is ResponseMessage rspRes)
			{
				if (rspRes.Error is ResponseError err)
				{
					writer.WritePropertyName("error");
					JsonSerializer.Serialize(writer, err, options);
				}
				else
					writer.WriteNull("result");
			}

			writer.WriteEndObject();
		}
	}
}

[JsonConverter(typeof(RequestMessage<>))]
internal class GenericRequestMessageJsonConverter<T>() : JsonConverter<RequestMessage<T>> where T : IRequestParams
{
	public override RequestMessage<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (JsonSerializer.Deserialize<Message>(ref reader, options) is not RequestMessage<T> res)
			throw new JsonException();

		return res;
	}

	public override void Write(Utf8JsonWriter writer, RequestMessage<T> value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WriteString("jsonrpc", "2.0");
		writer.WriteNumber("id", value.Id);
		writer.WriteString("method", value.Method);
		writer.WritePropertyName("params");
		JsonSerializer.Serialize(writer, value.Params, options);
		writer.WriteEndObject();
	}
}

[JsonConverter(typeof(ResponseMessage<>))]
internal class GenericResponseMessageJsonConverter<T>() : JsonConverter<ResponseMessage<T>> where T : IResult
{
	public override ResponseMessage<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (JsonSerializer.Deserialize<Message>(ref reader, options) is not ResponseMessage<T> res)
			throw new JsonException();

		return res;
	}

	public override void Write(Utf8JsonWriter writer, ResponseMessage<T> value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WriteString("jsonrpc", "2.0");
		writer.WriteNumber("id", value.Id);
		writer.WritePropertyName("result");
		JsonSerializer.Serialize(writer, value.Result, options);
		writer.WriteEndObject();
	}
}

[JsonConverter(typeof(NotificationMessage<>))]
internal class GenericNotificationMessageJsonConverter<T>() : JsonConverter<NotificationMessage<T>> where T : INotificationParams
{
	public override NotificationMessage<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (JsonSerializer.Deserialize<Message>(ref reader, options) is not NotificationMessage<T> res)
			throw new JsonException();

		return res;
	}

	public override void Write(Utf8JsonWriter writer, NotificationMessage<T> value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WriteString("jsonrpc", "2.0");
		writer.WriteString("method", value.Method);
		writer.WritePropertyName("params");
		JsonSerializer.Serialize(writer, value.Params, options);
		writer.WriteEndObject();
	}
}
