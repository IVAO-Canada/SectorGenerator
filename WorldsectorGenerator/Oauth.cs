using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Web;

namespace WorldsectorGenerator;
internal class Oauth : IDisposable
{
	readonly HttpListener _endpoint = new();
	readonly HttpClient _http = new();

	public Oauth()
	{
		_endpoint.Prefixes.Add("http://localhost:22125/");
		_endpoint.Start();
	}

	public async Task<JsonObject> GetOpenIdFromBrowserAsync()
	{
		string code;
		HttpListenerContext context;

		var proc = Process.Start(new ProcessStartInfo() {
			FileName = "https://sso.ivao.aero/authorize?response_type=code&client_id=d569e5a6-367c-4014-9892-8239f339bacc&redirect_uri=http%3A%2F%2Flocalhost%3A22125%2F&scope=openid",
			UseShellExecute = true
		});

		context = await _endpoint.GetContextAsync();
		if (context.Request.QueryString["code"] is not string tmpCode)
			return await GetOpenIdFromBrowserAsync();

		code = HttpUtility.UrlEncode(tmpCode);
		string html = @"<!DOCTYPE html>
<html>
	<head></head>
	<body><script type=""text/javascript"">close();</script></body>
</html>";
		byte[] htmlBytes = System.Text.Encoding.UTF8.GetBytes(html);
		await context.Response.OutputStream.WriteAsync(htmlBytes);
		context.Response.OutputStream.Close();

		var resp = await _http.PostAsync("https://api.ivao.aero/v2/oauth/token", JsonContent.Create(new
		{
			grant_type = "authorization_code",
			code,
			redirect_uri = "http://localhost:22125/",
			client_id = "d569e5a6-367c-4014-9892-8239f339bacc",
			client_secret = "qGDStCwskPyxfMjUyf9kFTbV8bgp5EQW"
		}));
		if (JsonNode.Parse(await resp.Content.ReadAsStringAsync()) is JsonObject retval)
			return retval;
		else
			return await GetOpenIdFromBrowserAsync();
	}

	public async Task<JsonObject> GetOpenIdFromRefreshTokenAsync(string refreshToken)
	{
		var resp = await _http.PostAsync("https://api.ivao.aero/v2/oauth/token", JsonContent.Create(new
		{
			grant_type = "refresh_token",
			client_id = "d569e5a6-367c-4014-9892-8239f339bacc",
			refresh_token = refreshToken
		}));
		if (JsonNode.Parse(await resp.Content.ReadAsStringAsync()) is JsonObject retval)
			return retval;
		else
			return await GetOpenIdFromBrowserAsync();
	}

	public void Dispose()
	{
		_endpoint.Stop();
	}
}
