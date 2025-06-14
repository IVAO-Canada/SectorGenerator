using ManualAdjustments.LSP;
using ManualAdjustments.LSP.Messages;

using System.CommandLine;
using System.Text.Json;

// Get on that STDIO right away, just in case!
Communicator communicator = new StdioCommunicator();

// Define command line settings.
RootCommand commandLineCommand = new("LSP implementation for the SectorFile Manual Adjustment File system.");

Option<bool> cliStdioOption = new(
	"--stdio",
	static () => true
) { IsHidden = true };

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
commandLineCommand.AddOption(cliStdioOption);

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

// Load all the IParams types into a usable dictionary.
InjectionContext.Shared.LoadParamTypes();

// Add the communicator so it can be pulled later where needed.
InjectionContext.Shared.Add(communicator);

// Add the document synchroniser.
InjectionContext.Shared.Add<DocumentManager>();

// Add the FAA data.
System.Diagnostics.Debugger.Launch();

if (Environment.GetEnvironmentVariable("LSP_CWD") is string lspDir)
	Environment.CurrentDirectory = lspDir;

InjectionContext.Shared.Add(CIFPReader.CIFP.Load("http://ivao-us.s3-website-us-west-2.amazonaws.com/reduced/"));
InjectionContext.Shared.Add(await FaaCycleData.LoadAsync());

// Add the JSON serialisation needs.
InjectionContext.Shared.Add<MessageJsonConverter>();
InjectionContext.Shared.Add<JsonSerializerOptions>(new(JsonSerializerDefaults.Web));
InjectionContext.Shared.Get<JsonSerializerOptions>().Converters.Add(InjectionContext.Shared.Get<MessageJsonConverter>());

await Server.RunAsync(communicator);
