using ManualAdjustments.LSP;

using System.CommandLine;

// Define command line settings.
RootCommand commandLineCommand = new("LSP implementation for the SectorFile Manual Adjustment File system.");

Option<string?> cliPipeOption = new(
	"--pipe",
	static () => null,
	OperatingSystem.IsWindows()
	? "Interface with the LSP through a named pipe."
	: "Interface with the LSP through a socket file."
);

Option<ushort?> cliPortOption = new(
	"--port",
	static () => null,
	"Interface with the LSP through a network socket on the designated port."
);

Option<int?> cliClientProcessId = new(
	"--clientProcessId",
	static () => null,
	"The PID of the client process. The server will shut down when the provided process terminates."
);

commandLineCommand.AddOption(cliPipeOption);
commandLineCommand.AddOption(cliPortOption);

Communicator communicator = new StdioCommunicator();

// Process the actual command entered.
commandLineCommand.SetHandler(ctx => {
	string? pipeName = ctx.ParseResult.GetValueForOption(cliPipeOption);
	ushort? portNumber = ctx.ParseResult.GetValueForOption(cliPortOption);
	int? processId = ctx.ParseResult.GetValueForOption(cliClientProcessId);

	if (pipeName is not null && portNumber is not null)
	{
		ctx.Console.Error.Write("Cannot specify multiple communication methods.");
		return Task.FromResult(-1);
	}
	else if (pipeName is string pipe)
		communicator = new NamedPipeCommunicator(pipe);
	else if (portNumber is ushort port)
		communicator = new SocketCommunicator(port);

	if (processId is int pid)
		System.Diagnostics.Process.GetProcessById(pid).Exited += (_, _) => throw new NotImplementedException(); // TODO! Kill the server.

	return Task.FromResult(0);
});

int cliRetCode = await commandLineCommand.InvokeAsync(args);

if (cliRetCode != 0)
{
	Environment.Exit(cliRetCode);
	throw new System.Diagnostics.UnreachableException();
}
