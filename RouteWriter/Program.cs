using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

using RouteWriter.Pages;

using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace RouteWriter;
public static class Program
{
	public static async Task Main(string[] args)
	{
		var builder = WebAssemblyHostBuilder.CreateDefault(args);

		builder.UseBrowserExtension(browserExtension =>
		{
			if (browserExtension.Mode == Blazor.BrowserExtension.BrowserExtensionMode.ContentScript)
			{
				builder.RootComponents.Add<ContentScript>("#route-writer-app");
			}
			else
			{
				builder.RootComponents.Add<App>("#app");
				builder.RootComponents.Add<HeadOutlet>("head::after");
			}
		});

		builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
		await builder.Build().RunAsync();
	}
}
