using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace WorldsectorGenerator;

internal class WebeyeGrabber(string token)
{
	readonly HttpClient _api = new() {
		BaseAddress = new("https://api.ivao.aero/"),
		DefaultRequestHeaders = {
			{ "Authorization", $"Bearer {token}" }
		}
	};

	public async Task<IEnumerable<(string Position, string Facility, Boundary Boundary)>> GetSubcentersAsync() =>
		(await _api.GetFromJsonAsync<WebeyeObject[]>("/v2/subcenters/all?mapType=regionMap"))!.Select(o => (o.Position, o.Facility, new Boundary(o.Boundary)));

	public async Task<IEnumerable<(string Position, string Facility, Boundary Boundary)>> GetAtcPositionsAsync() =>
		(await _api.GetFromJsonAsync<WebeyeObject[]>("/v2/ATCPositions/all?mapType=regionMap&loadAirport=false"))!.Select(o => (o.Position, o.Facility, new Boundary(o.Boundary)));

	private class WebeyeObject
	{
		[JsonPropertyName("regionMap")]
		public Point[] Boundary { get; set; } = [];

		[JsonPropertyName("composePosition")]
		public string Position { get; set; } = "";

		[JsonPropertyName("position")]
		public string Facility { get; set; } = "";
	}
}
