using System.Buffers;
using System.IO.Pipes;
using System.Net;
using System.Net.Sockets;

namespace ManualAdjustments.LSP;

internal abstract class Communicator
{
	/// <summary>Invoked when a <see langword="string"/> is received from the client.</summary>
	public event EventHandler<string>? MessageReceived;
	private StreamWriter? _writer;

	/// <summary>Sends a string to the client after adding the correct content headers.</summary>
	/// <param name="message">The <see langword="string"/> to send to the client.</param>
	public async Task SendAsync(string message)
	{
		if (_writer is null)
			throw new Exception("The stream must be set before attempting to send a message.");

		await _writer.WriteLineAsync($"Content-Length: {System.Text.Encoding.UTF8.GetByteCount(message)}");
		await _writer.WriteLineAsync();
		await _writer.WriteAsync(message);
		await _writer.FlushAsync();
	}

	protected Task SetStream(Stream stream, CancellationToken token) => SetStream(stream, stream, token);

	protected async Task SetStream(Stream inputStream, Stream outputStream, CancellationToken token)
	{
		_writer = new(outputStream);
		await ReadFromStreamAsync(inputStream, token).ConfigureAwait(false);
	}

	private async Task ReadFromStreamAsync(Stream stream, CancellationToken token)
	{
		using StreamReader reader = new(stream, System.Text.Encoding.UTF8);

		while (!token.IsCancellationRequested)
		{
			string contentType = "application/vscode-jsonrpc; charset=utf-8";
			int contentLength = -1;

			string? input;
			// Read until a blank line occurs.
			while ((input = reader.ReadLine()) is not "")
			{
				if (input is not string headerLine)
				{
					await SendAsync("Stream closed.");
					return;
				}

				if (headerLine.StartsWith("Content-Length: ") && int.TryParse(headerLine["Content-Length: ".Length..], out contentLength))
					continue;
				else if (headerLine.StartsWith("Content-Type: "))
					contentType = headerLine["Content-Type: ".Length..];
				else
				{
					await SendAsync($"Non-compliant header {headerLine}.");
					return;
				}
			}

			using IMemoryOwner<char> memOwner = MemoryPool<char>.Shared.Rent(contentLength);
			if (await reader.ReadAsync(memOwner.Memory[..contentLength], token) != contentLength)
			{
				await SendAsync("Buffering error.");
				return;
			}

			MessageReceived?.Invoke(this, memOwner.Memory[..contentLength].ToString());
		}
	}
}

internal class StdioCommunicator : Communicator, IDisposable
{
	private readonly CancellationTokenSource _inputLoopCancellation = new();
	private readonly Task _inputLoop;

	public StdioCommunicator() => _inputLoop = SetStream(
		Console.OpenStandardInput(),
		Console.OpenStandardOutput(),
		_inputLoopCancellation.Token
	);

	public void Dispose()
	{
		_inputLoopCancellation.Cancel();
		_inputLoop.Wait();
	}
}

internal class NamedPipeCommunicator : Communicator, IDisposable
{
	readonly NamedPipeClientStream _client;

	public NamedPipeCommunicator(string pipeName)
	{
		_client = new(".", pipeName, PipeDirection.InOut);
		_client.Connect(TimeSpan.FromSeconds(2));
	}

	public void Dispose() => _client.Dispose();
}

internal class SocketCommunicator : Communicator, IDisposable
{
	readonly TcpClient _client = new();

	public SocketCommunicator(ushort port) => _client.Connect(IPAddress.Loopback, port);

	public void Dispose() => _client.Dispose();
}
