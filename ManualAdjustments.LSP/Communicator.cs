using System.Buffers;
using System.IO.Pipes;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Metadata;

namespace ManualAdjustments.LSP;

internal abstract class Communicator : IDisposable
{
	/// <summary>Invoked when a <see langword="string"/> is received from the client.</summary>
	public event EventHandler<string>? MessageReceived;

	public bool Blocked { get; protected set; } = true;

	private StreamWriter? _writer;
	private readonly SemaphoreSlim _sendSemaphore = new(1);

	/// <summary>Sends a string to the client after adding the correct content headers.</summary>
	/// <param name="message">The <see langword="string"/> to send to the client.</param>
	public async Task SendAsync(string message)
	{
		if (_writer is null)
			throw new Exception("The stream must be set before attempting to send a message.");

		await _sendSemaphore.WaitAsync();

		try
		{
			await _writer.WriteLineAsync($"Content-Length: {System.Text.Encoding.UTF8.GetByteCount(message)}");
			await _writer.WriteLineAsync();
			await _writer.WriteAsync(message);
			await _writer.FlushAsync();
		}
		finally
		{
			_sendSemaphore.Release();
		}
	}

	protected Task SetStream(Stream stream, CancellationToken token) => SetStream(stream, stream, token);

	protected Task SetStream(Stream inputStream, Stream outputStream, CancellationToken token)
	{
		_writer = new(outputStream) { AutoFlush = false };
		return Task.Run(async () => await ReadFromStreamAsync(inputStream, token).ConfigureAwait(false), token);
	}

	private async Task ReadFromStreamAsync(Stream stream, CancellationToken token)
	{
		using StreamReader reader = new(stream, System.Text.Encoding.UTF8);

		while (!token.IsCancellationRequested)
		{
			while (Blocked && !token.IsCancellationRequested)
				await Task.Delay(100, token);

			if (token.IsCancellationRequested)
				return;

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

	public void Block() => Blocked = true;
	public void Unblock() => Blocked = false;

	public abstract void Dispose();
}

internal class StdioCommunicator : Communicator
{
	private readonly CancellationTokenSource _inputLoopCancellation = new();
	private readonly Task _inputLoop;

	public StdioCommunicator() => _inputLoop = SetStream(
		Console.OpenStandardInput(),
		Console.OpenStandardOutput(),
		_inputLoopCancellation.Token
	);

	public override void Dispose()
	{
		_inputLoopCancellation.Cancel();
		_inputLoop.Wait();
	}
}

internal class NamedPipeCommunicator : Communicator
{
	readonly NamedPipeClientStream _client;
	private readonly CancellationTokenSource _inputLoopCancellation = new();
	private readonly Task _inputLoop;

	public NamedPipeCommunicator(string pipeName)
	{
		string host = ".";
		if (pipeName.StartsWith(@"\\"))
		{
			host = pipeName[2..].Split('\\')[0];
			pipeName = pipeName[(2 + host.Length)..];

			if (!pipeName.StartsWith(@"\pipe\"))
				throw new NotImplementedException();

			pipeName = pipeName[@"\pipe\".Length..];
		}

		_client = new(host, pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
		_client.Connect();
		_inputLoop = SetStream(_client, _inputLoopCancellation.Token);
	}

	public override void Dispose()
	{
		_inputLoopCancellation.Dispose();
		_client.Dispose();
		_inputLoop.Wait();
	}
}

internal class SocketCommunicator : Communicator
{
	readonly TcpClient _client = new();

	public SocketCommunicator(ushort port)
	{
		Console.WriteLine($"Connecting to socket on port {port}.");
		_client.Connect(IPAddress.Loopback, port);
	}

	public override void Dispose() => _client.Dispose();
}
