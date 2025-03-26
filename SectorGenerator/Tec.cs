namespace SectorGenerator;
internal class Tec
{
	const string NFDC_PREF_ROUTES_URL = @"https://www.fly.faa.gov/rmt/data_file/prefroutes_db.csv";
	static readonly HttpClient _http = new();

	public static async Task<TecRoute[]> GetRoutesAsync()
	{
		string[] allLines = (await _http.GetStringAsync(NFDC_PREF_ROUTES_URL)).Split("\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		string[][] routeFields = [.. allLines.Skip(1).Select(l => l.Split(','))];

		string[][] tecRouteFields = [..routeFields
			.Where(f => f[6] == "TEC" && (f[12] == "ZLA" || f[13] == "ZLA"))
			.DistinctBy(f => f[10])];

		return [..
			tecRouteFields
			.Select(tec => new TecRoute(tec[10], [..tec[1].Split().Skip(1).SkipLast(1)]))
			.Where(r => r.Route.Length > 0)
		];
	}

	internal record TecRoute(string Name, string[] Route);
}
