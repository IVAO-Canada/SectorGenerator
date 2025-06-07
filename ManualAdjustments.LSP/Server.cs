using ManualAdjustments.LSP.Messages;

using System.Collections.Concurrent;

namespace ManualAdjustments.LSP;

internal class Server : IDisposable
{
	readonly Communicator _client;
	readonly BlockingCollection<Message> _messageQueue = new(64);

	public Server(Communicator communicator)
	{
		_client = communicator;
		_client.MessageReceived += Client_MessageReceived;
	}

	private async void Client_MessageReceived(object? sender, string messageJson) => throw new NotImplementedException();

	public void Dispose() => _client.Dispose();
}
