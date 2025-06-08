import { workspace, ExtensionContext } from 'vscode';
import {LanguageClient, LanguageClientOptions, ServerOptions, TransportKind} from 'vscode-languageclient/node'

let client: LanguageClient;

export function activate(context: ExtensionContext) {
	const serverOptions: ServerOptions = {
		run: {
			command: 'C:\\Users\\westo\\OneDrive\\Flying\\IVAO\\SectorUtils\\ManualAdjustments.LSP\\bin\\Release\\net9.0\\ManualAdjustments.LSP.exe',
			transport: TransportKind.pipe
		},
		debug: {
			command: 'C:\\Users\\westo\\OneDrive\\Flying\\IVAO\\SectorUtils\\ManualAdjustments.LSP\\bin\\Debug\\net9.0\\ManualAdjustments.LSP.exe',
			transport: TransportKind.stdio
		}
	};

	const clientOptions: LanguageClientOptions = {
		documentSelector: [
			{ scheme: 'file', language: 'adjustment' }
		],
		synchronize: {
			fileEvents: workspace.createFileSystemWatcher('**/*.maf')
		}
	};

	client = new LanguageClient(
		'auroraMaf',
		'Aurora Manual Adjustment File Server',
		serverOptions,
		clientOptions
	);

	client.start();
}

export function deactivate(): Thenable<void> | undefined {
	if (!client)
		return undefined;

	return client.stop();
}
