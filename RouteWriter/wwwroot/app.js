export function beforeStart(options, extensions, blazorBrowserExtension) {
	if (blazorBrowserExtension.BrowserExtension.Mode === blazorBrowserExtension.Modes.ContentScript) {
		const appDiv = document.createElement("div");
		appDiv.id = "route-writer-app";
		document.body.appendChild(appDiv);

		const stylesheet = document.createElement("link");
		stylesheet.rel = "stylesheet";
		stylesheet.type = "text/css";
		stylesheet.href = browser.runtime.getURL("css/skyvector.css");
		document.head.appendChild(stylesheet);

		window.addEventListener("message", (e) => {
			if (e.origin.startsWith('https://skyvector.com') && typeof(e.data) === "string" && e.data.startsWith('/'))
				messageReceived(e.data.slice(1));
		});
	}
}

function messageReceived(message) {
	const extensionEdit = document.getElementById('route-definition-editor');
	const planEditDiv = document.getElementById('sv_planEdit');
	const planEditField = document.getElementById('sv_planEditField');
	const clearPlan = document.querySelector('a.svfpl_iconlinkbtn');
	clearPlan.click();
	setTimeout(() => {
		planEditField.focus();
		document.execCommand('insertText', false, message);
		setTimeout(() => {
			planEditDiv.click();
			extensionEdit.focus();
		}, 250);
	}, 500);
}