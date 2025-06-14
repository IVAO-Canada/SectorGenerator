import { commands, window, workspace, ExtensionContext, ViewColumn, WebviewPanel, TextEditorSelectionChangeKind } from 'vscode';
import {LanguageClient, LanguageClientOptions, ServerOptions, TransportKind} from 'vscode-languageclient/node'

let client: LanguageClient;

export function activate(context: ExtensionContext) {
	context.subscriptions.push(
		commands.registerCommand('aurora-maf.preview', () => {
			const panel = window.createWebviewPanel(
				'aurora-maf',
				'Aurora MAF Preview',
				ViewColumn.Beside,
				{
					enableScripts: true
				}
			)
			
			setupPanel(panel, context);
		})
	);

	const serverOptions: ServerOptions = {
		run: {
			command: `${context.extensionPath}/bin/maf-lsp`,
			options: {
				cwd: context.extensionPath
			},
			transport: TransportKind.pipe
		},
		debug: {
			command: 'C:\\Users\\westo\\OneDrive\\Flying\\IVAO\\SectorUtils\\ManualAdjustments.LSP\\bin\\Debug\\net9.0\\maf-lsp.exe',
			options: {
				cwd: context.extensionPath,
				env: {
					LSP_CWD: context.extensionPath
				}
			},
			transport: TransportKind.pipe
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

	client.onNotification('$/imagePreview', (data: { image: string }) => {
		image = data.image;
	});

	window.onDidChangeTextEditorSelection((e) => {
		if (e.kind !== TextEditorSelectionChangeKind.Mouse || e.selections.length <= 0 || !e.textEditor.document.uri)
			return;

		client.sendNotification('$/selectionChanged', {
			textDocument: { uri: e.textEditor.document.uri.toString() },
			positions: Array(...e.selections.map(s => s.start))
		})
	}, null, context.subscriptions)

	client.start();
}

export function deactivate(): Thenable<void> | undefined {
	if (!client)
		return undefined;

	return client.stop();
}

function setupPanel(panel: WebviewPanel, context: ExtensionContext) {
	let lastImage = image;
	panel.webview.html = getWebviewContent();

	const interval = setInterval(() => {
		if (image !== lastImage) {
			panel.webview.postMessage({
				command: 'updatePreview',
				image
			});

			lastImage = image;
		}
	}, 1000);

	panel.onDidDispose(() => { clearInterval(interval); }, null, context.subscriptions)
}

// image must be a base64 encoded image string: Ex: "data:image/png;base64,…"
let image: string = '';

function getWebviewContent() : string {
	return `
	<!DOCTYPE html>
	<html lang="en">
		<head>
			<meta charset="UTF-8" />
			<meta name="viewport" content="width=device-width, initial-scale=1.0" />
			<meta http-equiv="Content-Security-Policy" content="default-src 'none'; img-src data:; script-src 'unsafe-inline';" />
		</head>
		<body>
			<img id="preview" src="${image}" alt="Rendered Preview" style="max-width: 100%; height: auto;" />
			<script>
				window.addEventListener('message', event => {
					const message = event.data;

					if (message.command === 'updatePreview') {
						document.getElementById('preview').src = message.image;
					}
				});
			</script>
		</body>
	</html>
	`
}