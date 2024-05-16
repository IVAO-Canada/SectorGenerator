using System.Text.RegularExpressions;

namespace SectorGenerator;

internal static class ArtccBoundaries
{
	static readonly HttpClient _http = new();

	const string ZNY_OCEANIC_INJECT = @"ZNY,0,2000,36420900N,072395800W,ZDC
ZNY,0,2000,35054000N,072395800W,ZWY
ZNY,0,2000,32120700N,076490600W,ZWY
ZNY,0,3000,32154400N,077000000W,ZJX
ZNY,0,3000,32580400N,076464600W,ZJX
ZNY,0,2000,33250900N,076285200W,ZDC
ZNY,0,2000,34360100N,075410200W,ZDC
ZNY,0,2000,35182900N,075112400W,ZDC
ZNY,0,2000,34140000N,073570000W,ZDC
ZNY,0,2000,34214600N,073450600W,ZDC
ZNY,0,2000,34291700N,073342300W,ZDC
ZNY,0,2000,35294900N,074561300W,ZDC
ZNY,0,2000,36471600N,074355900W,ZDC
ZNY,0,2000,36470181N,074292976W,ZDC
ZNY,0,2000,36420900N,072395800W,ZDC";

	const string ZMA_TEG_BODLO_INJECT = @"ZMA,0,3000,20250000N,073000000W,TEG";

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
			List<string> boundaryLines = [..(await _http.GetStringAsync(link)).Split("\r\n")];
			int idx = boundaryLines.IndexOf("ZNY,0,2000,36420900N,072395800W,ZDC");
			boundaryLines[idx] = ZNY_OCEANIC_INJECT;

			idx = boundaryLines.IndexOf("ZMA,0,3000,21142100N,067390200W,ZWY");
			boundaryLines.Insert(++idx, "ZSU" + boundaryLines[idx - 1][3..^3] + "ZMA");

			while (boundaryLines[++idx] != "ZMA,0,3000,19000000N,068000000W,DCS")
				boundaryLines[idx] = "ZSU" + boundaryLines[idx][3..];

			boundaryLines.Insert(idx, "ZSU" + boundaryLines[idx][3..^3] + "ZMA");

			idx = boundaryLines.IndexOf("ZMA,0,3000,20250000N,071400000W,DCS") + 1;
			while (boundaryLines[idx] != "ZMA,0,3000,20000000N,073200000W,HAV")
				boundaryLines.RemoveAt(idx);

			boundaryLines.Insert(idx, ZMA_TEG_BODLO_INJECT);

			boundaryFileContents = string.Join("\r\n", boundaryLines);
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
