using System.Text.RegularExpressions;

namespace SectorGenerator;

internal static class ArtccBoundaries
{
	static readonly HttpClient _http = new();

	public static async Task<(Dictionary<string, (double Latitude, double Longitude)[]> Boundaries, Dictionary<string, string[]> Neighbours, string[] Faa)> GetBoundariesAsync(string? link = null)
	{
		string boundaryFileContents;

		if (link is null && File.Exists("artccBoundaries.csv") && new FileInfo("artccBoundaries.csv").LastWriteTime >= DateTime.Now.AddDays(-5))
			boundaryFileContents = File.ReadAllText("artccBoundaries.csv");
		else if (link is null)
		{
			Regex eramLink = new(@"https://aeronav.faa.gov/Upload_313-d/ERAM_ARTCC_Boundaries/Ground_Level_ARTCC_Boundary_Data_\d+-\d+-\d+.csv", RegexOptions.CultureInvariant);

			string[] allLinks =
				eramLink.Matches(await _http.GetStringAsync(@"https://www.faa.gov/air_traffic/flight_info/aeronav/Aero_Data/Center_Surface_Boundaries/"))
				.Select(m => m.Value)
				.ToArray();

			link = allLinks.Order().Last();
			boundaryFileContents = await _http.GetStringAsync(link);
			File.WriteAllText("artccBoundaries.csv", boundaryFileContents);
		}
		else
			boundaryFileContents = await _http.GetStringAsync(link);

		Dictionary<string, string[][]> boundaryPoints =
			boundaryFileContents
			.Split("\r\n")
			.Where(l => l.Length > 3 && l[0] == 'Z' && l[3] == ',')
			.GroupBy(l => l.Split(',')[0])
			.ToDictionary(g => g.Key, g => g.Select(l => l.Split(',')).ToArray());

		foreach (string ctr in boundaryPoints.Values.SelectMany(v => v.Select(l => l[5])).Distinct().ToArray())
		{
			if (boundaryPoints.ContainsKey(ctr))
				continue;

			string[][] points = [..boundaryPoints.Values.Select(v => v.Where(l => l[5] == ctr).ToArray()).Where(g => g.Length > 0).SelectMany(i => i).Distinct()];
			boundaryPoints.Add(ctr, points);
		}

		return (
			boundaryPoints.Select(b => new KeyValuePair<string, (double, double)[]>(
				b.Key,
				b.Value.Select(l => (
					ConvertEramCoordPartToDouble(l[3]),
					ConvertEramCoordPartToDouble(l[4])
				)).ToArray()
			)).ToDictionary(),

			boundaryPoints.Select(b => new KeyValuePair<string, string[]>(
				b.Key,
				b.Value.Select(l => l[5]).Distinct().ToArray()
			)).ToDictionary(),

			[..boundaryFileContents.Split("\r\n").Where(l => l.Length > 3 && l[0] == 'Z' && l[3] == ',').Select(l => l.Split(',')[0]).Distinct()]
		);
	}

	static double ConvertEramCoordPartToDouble(string eramCoordPart)
	{
		int degrees = int.Parse(eramCoordPart[..^7]);
		int minutes = int.Parse(eramCoordPart[^7..^5]);
		double seconds = double.Parse(eramCoordPart[^5..^3] + "." + eramCoordPart[^3..^1]);

		return (degrees + ((minutes + seconds / 60) / 60)) * (eramCoordPart[^1] is 'S' or 'W' ? -1 : 1);
	}
}
