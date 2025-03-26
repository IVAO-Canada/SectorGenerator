using Google.Protobuf.Reflection;

using System.Net.Http.Json;

namespace SectorGenerator;

internal static class ArtccBoundaries
{
	static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(30) };

	public static async Task<(Dictionary<string, HashSet<(double Latitude, double Longitude)[]>> Boundaries, Dictionary<string, string[]> Neighbours, string[] Faa)> GetBoundariesAsync()
	{
		if (await _http.GetFromJsonAsync<GisData>("https://services6.arcgis.com/ssFJjBXIUyZDrSYZ/arcgis/rest/services/Boundary_Airspace/FeatureServer/0/query?where=1%3D1&outFields=NAME,TYPE_CODE,LEVEL_,COUNTRY,IDENT&outSR=4326&f=json") is not GisData gd)
			return ([], [], []);

		gd.features = [..
			gd.features.Where(f =>
				f.attributes.IDENT is not null && f.attributes.IDENT != "ZAN" &&
				f.attributes.TYPE_CODE is "ARTCC" or "FIR" or "CTA-P" or "ACC" &&
				f.attributes.LEVEL_ is "B" or "L" &&
				((f.attributes.IDENT.Length == 3 && f.attributes.TYPE_CODE == "ARTCC") || (f.attributes.IDENT.Length == 4 && f.attributes.TYPE_CODE != "ARTCC"))
			)
		];

		Dictionary<string, HashSet<(double Latitude, double Longitude)[]>> boundaries = [];

		foreach (var featureGroup in gd.features.GroupBy(s => s.attributes.IDENT!))
			boundaries.Add(featureGroup.Key, [..featureGroup.SelectMany(g => g.geometry.rings.Select(r => r.Select(p => (p[1], p[0])).ToArray()))]);

		Dictionary<string, HashSet<(double Latitude, double Longitude)>> allPointsInBoundary =
			boundaries.Select(kvp => new KeyValuePair<string, HashSet<(double Latitude, double Longitude)>>(kvp.Key, [..kvp.Value.SelectMany(b => b)])).ToDictionary();

		return (
			boundaries,
			allPointsInBoundary.ToDictionary(kvp => kvp.Key, kvp => allPointsInBoundary.Keys.Where(k => k != kvp.Key && allPointsInBoundary[k].Intersect(kvp.Value).Any()).ToArray()),
			[..gd.features.Where(f => boundaries.ContainsKey(f.attributes.IDENT ?? "") && f.attributes.COUNTRY == "United States").Select(f => f.attributes.IDENT).Distinct()]
		);
	}


#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
	public class GisData
	{
		public string objectIdFieldName { get; set; }
		public string globalIdFieldName { get; set; }
		public string geometryType { get; set; }
		public Feature[] features { get; set; }
	}

	public class Feature
	{
		public Attributes attributes { get; set; }
		public Geometry geometry { get; set; }
	}

	public class Attributes
	{
		public string? IDENT { get; set; }
		public string NAME { get; set; }
		public string TYPE_CODE { get; set; }
		public string LEVEL_ { get; set; }
		public string COUNTRY { get; set; }
	}

	public class Geometry
	{
		public double[][][] rings { get; set; }
	}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
#pragma warning restore IDE1006 // Naming Styles
}
