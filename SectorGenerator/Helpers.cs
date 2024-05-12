using System.Net.Http.Json;
using System.Text.Json.Nodes;

using WSleeman.Osm;

namespace SectorGenerator;

internal static class Helpers
{
	/// <seealso cref="https://stackoverflow.com/a/218081/8443457"/>
	public static bool CheckIntersection(((double Latitude, double Longitude) Start, (double Latitude, double Longitude) End) segment1, ((double Latitude, double Longitude) Start, (double Latitude, double Longitude) End) segment2)
	{
		// True is clockwise.
		static bool orientation((double Latitude, double Longitude) p, (double Latitude, double Longitude) q, (double Latitude, double Longitude) r) =>
			((q.Latitude - p.Latitude) * (r.Longitude - q.Longitude) - (q.Longitude - p.Longitude) * (r.Latitude - q.Latitude)) > 0;

		bool o1 = orientation(segment1.Start, segment1.End, segment2.Start),
			 o2 = orientation(segment1.Start, segment1.End, segment2.End),
			 o3 = orientation(segment2.Start, segment2.End, segment1.Start),
			 o4 = orientation(segment2.Start, segment2.End, segment1.End);

		return o1 != o2 && o3 != o4;
	}

	public static bool IsInPolygon(Way polygon, (double Latitude, double Longitude) point) => IsInPolygon(
		[.. polygon.Nodes.Select(n => (n.Latitude, n.Longitude))],
		(point.Latitude, point.Longitude)
	);

	/// <seealso cref="https://stackoverflow.com/a/218081/8443457"/>
	public static bool IsInPolygon((double Latitude, double Longitude)[] polygon, (double Latitude, double Longitude) point)
	{
		double minLat = polygon.Min(p => p.Latitude), maxLat = polygon.Max(p => p.Latitude),
			  minLon = polygon.Min(p => p.Longitude), maxLon = polygon.Max(p => p.Longitude);

		if (point.Latitude < minLat || point.Latitude > maxLat
		 || point.Longitude < minLon || point.Longitude > maxLon)
			return false;

		var checkVec = (
			(point.Latitude, minLon - 1),
			point
		);

		int intersections = 0;
		foreach (var side in polygon.Zip(polygon[1..].Append(polygon[0])))
			if (CheckIntersection(side, checkVec))
				++intersections;

		return intersections % 2 == 1;
	}

	public static async Task<JsonObject[]> GetAtcPositionsAsync(string token, string prefix, IEnumerable<string> centers)
	{
		HttpClient http = new();
		http.DefaultRequestHeaders.Authorization = new("Bearer", token);
		http.BaseAddress = new(@"https://api.ivao.aero");
		HashSet<JsonObject> positions = [..(await http.GetFromJsonAsync<JsonArray>("/v2/ATCPositions/all?loadAirport=false"))!.Cast<JsonObject>()];
		JsonObject[] centerObjs = (await http.GetFromJsonAsync<JsonObject[]>($"/v2/positions/search?startsWith={prefix}&positionType=CTR&limit=100"))!;

		return [.. positions.Where(jo => { string pos = jo["composePosition"]!.GetValue<string>(); return pos.StartsWith(prefix); }).Concat(centerObjs)];
	}

	public static string Dms(double value, bool longitude) =>
		longitude
		? $"{(value >= 0 ? 'E' : 'W')}{(int)Math.Abs(value):000}.{(int)(Math.Abs(value) * 60) % 60:00}.{Math.Abs(value) * 360 % 60:00.000}"
		: $"{(value >= 0 ? 'N' : 'S')}{(int)Math.Abs(value):00}.{(int)(Math.Abs(value) * 60) % 60:00}.{Math.Abs(value) * 360 % 60:00.000}";

	public static string DMS(decimal value, bool longitude) => Dms((double)value, longitude);

	/// <summary>
	/// Gets the distance in nautical miles between two <see cref="OsmSharp.Node">Nodes</see>.
	/// </summary>
	public static double? DistanceTo(this Node from, Node to)
	{
		const double DEG_TO_RAD = Math.PI / 180;

		double dLon = (to.Longitude - from.Longitude) * DEG_TO_RAD;

		double dSigma = Math.Acos(
			Math.Sin(from.Latitude * DEG_TO_RAD) * Math.Sin(to.Latitude * DEG_TO_RAD) +
			Math.Cos(from.Latitude * DEG_TO_RAD) * Math.Cos(to.Latitude * DEG_TO_RAD) * Math.Cos(dLon)
		);

		return dSigma * 3443.9185;
	}

	/// <summary>
	/// Gets a very rough approximate distance in nautical miles between two <see cref="OsmSharp.Node">Nodes</see>.
	/// </summary>
	public static double ApproxDistanceSquaredTo(this Node from, Node to)
	{
		const double DEG_TO_RAD = Math.PI / 180;
		const double SQUARED_RAD = 3443.9185 * 3443.9185;

		double dLon = (to.Longitude - from.Longitude) * DEG_TO_RAD,
			   dLat = (to.Latitude - from.Latitude) * DEG_TO_RAD;

		return (dLon * dLon + dLat * dLat) * SQUARED_RAD;
	}

	public static readonly Dictionary<string, string> TraconCenters = new() {
		{ "KPCT", "KZDC" },
		{ "KSCT", "KZLA" },
		{ "KNCT", "KZOA" },
		{ "KN90", "KZNY" },
		{ "KL30", "KZLA" },
		{ "KA80", "KZTL" },
		{ "KI90", "KZHU" },
		{ "KJSD", "KZNY" }, // Damn you Sikorsky
		{ "KMUI", "KZNY" }
	};

	public static Way Inflate(this Way w, double radius)
	{
		if (w.Nodes.Length < 2)
			return w with { Nodes = w.Nodes };

		Node[] newPoints = new Node[w.Nodes.Length * 2];

		static (double dY, double dX) NormalBisectClockwise((double dY, double dX) prevVec, (double dY, double dX) nextVec, double referenceLat)
		{
			double skew = Math.Cos(referenceLat * Math.PI / 180);
			prevVec = (prevVec.dY, prevVec.dX * skew);
			nextVec = (nextVec.dY, nextVec.dX * skew);

			double dot = prevVec.dX * nextVec.dX + prevVec.dY * nextVec.dY;
			double det = prevVec.dX * nextVec.dY - prevVec.dY * nextVec.dX;
			double angleBetweenVectorsRad = Math.Atan2(-det, -dot) + Math.PI;
			var bisectRotationRad = Math.SinCos(angleBetweenVectorsRad / 2);

			var bisectVector = (
				dY: prevVec.dY * bisectRotationRad.Cos - prevVec.dX * bisectRotationRad.Sin,
				dX: prevVec.dX * bisectRotationRad.Cos + prevVec.dY * bisectRotationRad.Sin
			);

			double bisectVectorLength = Math.Sqrt(bisectVector.dY * bisectVector.dY + bisectVector.dX * bisectVector.dX);

			if (bisectVectorLength == 0)
				return (0, 0);
			else
				return (
					bisectVector.dY / bisectVectorLength,
					bisectVector.dX / bisectVectorLength
				);
		}


		for (int nodeIdx = 0; nodeIdx < w.Nodes.Length; ++nodeIdx)
		{
			(double Lat, double Lon) vecToNext, vecToPrev;

			if (nodeIdx < w.Nodes.Length - 1)
				vecToNext = (w.Nodes[nodeIdx + 1].Latitude - w.Nodes[nodeIdx].Latitude, w.Nodes[nodeIdx + 1].Longitude - w.Nodes[nodeIdx].Longitude);
			else
				vecToNext = (0, 0); // Keep the compiler happy.

			if (nodeIdx > 0)
				vecToPrev = (w.Nodes[nodeIdx - 1].Latitude - w.Nodes[nodeIdx].Latitude, w.Nodes[nodeIdx - 1].Longitude - w.Nodes[nodeIdx].Longitude);
			else
				vecToPrev = (-vecToNext.Lat, -vecToNext.Lon);

			if (nodeIdx >= w.Nodes.Length - 1)
				vecToNext = (-vecToPrev.Lat, -vecToPrev.Lon);

			var (dY, dX) = NormalBisectClockwise(vecToPrev, vecToNext, w.Nodes[0].Latitude);

			newPoints[nodeIdx] = new(
				Id: 0,
				Latitude: w.Nodes[nodeIdx].Latitude + (dY * radius),
				Longitude: w.Nodes[nodeIdx].Longitude + (dX * radius),
				Tags: System.Collections.Frozen.FrozenDictionary<string, string>.Empty
			);

			newPoints[newPoints.Length - nodeIdx - 1] = new(
				Id: 0,
				Latitude: w.Nodes[nodeIdx].Latitude - (dY * radius),
				Longitude: w.Nodes[nodeIdx].Longitude - (dX * radius),
				Tags: System.Collections.Frozen.FrozenDictionary<string, string>.Empty
			);
		}

		return w with { Nodes = newPoints };
	}

	public static Way? TryFabricateBoundary(this Relation r)
	{
		HashSet<Way> rWays = [.. r.Members.Where(i => i is Way w && w.Nodes.Length > 1).Cast<Way>()];

		if (rWays.Count == 0)
			return null;

		Way seed = rWays.MaxBy(w => w.Nodes.Length)!;
		rWays.Remove(seed);

		while (rWays.Where(w =>
				w.Nodes[0] == seed.Nodes[0] ||
				w.Nodes[0] == seed.Nodes[^1] ||
				w.Nodes[^1] == seed.Nodes[0] ||
				w.Nodes[^1] == seed.Nodes[^1]
			).MaxBy(w => w.Nodes.Length) is Way addWay)
		{
			rWays.Remove(addWay);
			if (addWay.Nodes[0] == seed.Nodes[0])
				seed = seed with { Nodes = [.. addWay.Nodes.Reverse().Concat(seed.Nodes[1..])] };
			else if (addWay.Nodes[0] == seed.Nodes[^1])
				seed = seed with { Nodes = [.. seed.Nodes.Concat(addWay.Nodes[1..])] };
			else if (addWay.Nodes[^1] == seed.Nodes[0])
				seed = seed with { Nodes = [.. addWay.Nodes.Concat(seed.Nodes[1..])] };
			else
				seed = seed with { Nodes = [.. seed.Nodes.Concat(addWay.Nodes[..^1].Reverse())] };
		}

		return seed with { Tags = r.Tags };
	}
}

public class Spinner : IDisposable
{
	private bool _disposed = false;
	TimeSpan _delay;

	public static Spinner Default => new(TimeSpan.FromSeconds(0.5), " -", " \\", " |", " /");

	public Spinner(TimeSpan delay, params string[] states) =>
		Task.Run(async () =>
		{
			_delay = delay;
			int lastStateLength = 0;
			while (true)
				foreach (string state in states)
				{
					if (_disposed)
						goto done;

					Console.Write(new string('\b', lastStateLength) + state);
					lastStateLength = state.Length;
					await Task.Delay(delay);
				}

			done:
			Console.Write(new string('\b', lastStateLength) + new string(' ', lastStateLength) + new string('\b', lastStateLength));
		});

	public void Dispose()
	{
		_disposed = true;
		Thread.Sleep(_delay);
	}
}