using ManualAdjustments.LSP.Messages;

using System.Collections.Concurrent;
using System.Text.Json;

namespace ManualAdjustments.LSP;

internal interface IResponseCollector
{
	public Task<T> GetResponseAsync<T>() where T : ResponseMessage;
	public Task<ResponseMessage> GetResponseAsync(Func<ResponseMessage, bool> filter);
}

internal class Server : IDisposable, IResponseCollector
{
	private event Action<ResponseMessage>? ResponseReceived;

	readonly Communicator _client;
	readonly BlockingCollection<Message> _messageQueue = new(64);
	readonly TaskCompletionSource _disposalTask;
	readonly JsonSerializerOptions _jsonOptions;

	public static async Task RunAsync(Communicator communicator)
	{
		TaskCompletionSource tcs = new();
		using Server server = new(communicator, tcs);
		await Task.WhenAll(tcs.Task, server.ProcessMessageQueueAsync());
	}

	private Server(Communicator communicator, TaskCompletionSource tcs)
	{
		_jsonOptions = InjectionContext.Shared.Get<JsonSerializerOptions>();
		InjectionContext.Shared.Add(this);

		_disposalTask = tcs;
		_client = communicator;
		_client.MessageReceived += Client_MessageReceived;

#if DEBUG
		if (!System.Diagnostics.Debugger.IsAttached)
			System.Diagnostics.Debugger.Launch();
#endif

		_client.Unblock();
	}

	private void Client_MessageReceived(object? sender, string messageJson)
	{
		if (JsonSerializer.Deserialize<Message>(messageJson, _jsonOptions) is not Message msg)
			// Invalid message.
			return;

		// Any extra validation logic required can be done here.

		// Add the message to the queue to be processed.
		_messageQueue.Add(msg);
	}

	private async Task ProcessMessageQueueAsync()
	{
		JsonSerializerOptions jsonOpts = InjectionContext.Shared.Get<JsonSerializerOptions>();
		foreach (Message message in _messageQueue.GetConsumingEnumerable())
		{
			if (message is RequestMessage req)
			{
				ResponseMessage result;

				if (InjectionContext.Shared.GetReqHandler(req.Method) is RequestMessageHandler handler)
					// Call the registered handler.
					result = await handler(req);
				else
					// No handler for it. Ah well!
					result = new ResponseMessage(req.Id, new(Types.ErrorCode.MethodNotFound, $"Method {req.Method} is not handled.", null));

				string msgJsonText = JsonSerializer.Serialize((Message)result, jsonOpts);
				await _client.SendAsync(msgJsonText);
			}
			else if (message is ResponseMessage rsp)
				ResponseReceived?.Invoke(rsp);
			else if (message is NotificationMessage note)
			{
				if (InjectionContext.Shared.GetNoteHandler(note.Method) is NotificationMessageHandler handler)
					await handler(note);
				else
					// No handler for it. Ah well!
					System.Diagnostics.Debug.WriteLine($"No handler for notification {note.Method}!");
			}
		}
	}

	public async Task<T> GetResponseAsync<T>() where T : ResponseMessage => (T)await GetResponseAsync(static resp => resp.GetType() == typeof(T));

	public Task<ResponseMessage> GetResponseAsync(Func<ResponseMessage, bool> filter)
	{
		TaskCompletionSource<ResponseMessage> completionSource = new();
		void OnResponse(ResponseMessage response)
		{
			if (!filter(response))
				return;

			completionSource.SetResult(response);
			ResponseReceived -= OnResponse;
		}

		ResponseReceived += OnResponse;
		return completionSource.Task;
	}

	public void Dispose()
	{
		if (_disposalTask.Task.IsCompleted)
			// Already disposed.
			return;

		_client.Dispose();
		_client.MessageReceived -= Client_MessageReceived;
		_messageQueue.CompleteAdding();
		_disposalTask.SetResult();
	}
}
