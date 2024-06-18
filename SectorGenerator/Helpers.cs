using CIFPReader;

using System.Collections.Frozen;
using System.Diagnostics;
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

		if (maxLon - minLon > 180)
			return IsInPolygon([.. polygon.Select(p => (p.Latitude, (p.Longitude + 360) % 360))], (point.Latitude, (point.Longitude + 360) % 360));

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

	public static async Task<JsonObject[]> GetAtcPositionsAsync(string token, params string[] prefixes)
	{
		HttpClient http = new();
		http.DefaultRequestHeaders.Authorization = new("Bearer", token);
		http.BaseAddress = new(@"https://api.ivao.aero");
		HashSet<JsonObject> positions = [.. (await http.GetFromJsonAsync<JsonArray>("/v2/ATCPositions/all?loadAirport=false"))!.Cast<JsonObject>()];
		List<JsonObject> centerObjs = [];

		foreach (string p in prefixes)
			centerObjs.AddRange((await http.GetFromJsonAsync<JsonObject[]>($"/v2/positions/search?startsWith={p}&positionType=CTR&limit=100"))!);

		return [.. positions.Where(jo => { string pos = jo["composePosition"]!.GetValue<string>(); return prefixes.Any(p => pos.StartsWith(p)); }).Concat(centerObjs)];
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
		{ "KPCT", "ZDC" },
		{ "KSCT", "ZLA" },
		{ "KNCT", "ZOA" },
		{ "KN90", "ZNY" },
		{ "KL30", "ZLA" },
		{ "KA80", "ZTL" },
		{ "KI90", "ZHU" },
		{ "KJSD", "ZNY" }, // Damn you Sikorsky
		{ "KMUI", "ZNY" }
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

	public static IEnumerable<Way> WaysAndBoundaries(this Osm osm) =>
		osm.Ways.Values.Concat(
			osm.Relations.Values.Select(r => r.TryFabricateBoundary()).Where(w => w is not null).Cast<Way>()
		);



	/// <summary>
	/// Returns a <see cref="Coordinate"/> which is a given <paramref name="distance"/> along a given <paramref name="bearing"/> from <see langword="this"/>.
	/// </summary>
	/// <param name="bearing">The true <see cref="Bearing"/> from <see langword="this"/>.</param>
	/// <param name="distance">The distance (in nautical miles) from <see langword="this"/>.</param>
	[DebuggerStepThrough]
	public static Node FixRadialDistance(this Node origin, double heading, double distance)
	{
		// Vincenty's formulae
		const double a = 3443.918;
		const double b = 3432.3716599595;
		const double f = 1 / 298.257223563;
		const double DEG_TO_RAD = Math.Tau / 360;
		const double RAD_TO_DEG = 360 / Math.Tau;
		static double square(double x) => x * x;
		static double cos(double x) => Math.Cos(x);
		static double sin(double x) => Math.Sin(x);

		double phi1 = origin.Latitude * DEG_TO_RAD;
		double L1 = origin.Longitude * DEG_TO_RAD;
		double alpha1 = heading * DEG_TO_RAD;
		double s = distance;

		double U1 = Math.Atan((1 - f) * Math.Tan(phi1));

		double sigma1 = Math.Atan2(Math.Tan(U1), cos(alpha1));
		double alpha = Math.Asin(cos(U1) * sin(alpha1));

		double uSquared = square(cos(alpha)) * ((square(a) - square(b)) / square(b));
		double A = 1 + (uSquared / 16384) * (4096 + uSquared * (-768 + uSquared * (320 - 175 * uSquared)));
		double B = (uSquared / 1024) * (256 + uSquared * (-128 + uSquared * (74 - 47 * uSquared)));

		double sigma = s / b / A,
			   oldSigma = sigma - 100;

		double twoSigmaM = double.NaN;

		while (Math.Abs(sigma - oldSigma) > 1.0E-9)
		{
			twoSigmaM = 2 * sigma1 + sigma;

			double cos_2_sigmaM = cos(twoSigmaM);

			double deltaSigma = B * sin(sigma) * (
					cos_2_sigmaM + 0.25 * B * (
						cos(sigma) * (
							-1 + 2 * square(cos_2_sigmaM)
						) - (B / 6) * cos_2_sigmaM * (
							-3 + 4 * square(sin(sigma))
						) * (
							-3 + 4 * square(cos_2_sigmaM)
						)
					)
				);
			oldSigma = sigma;
			sigma = s / b / A + deltaSigma;
		}

		(double sin_sigma, double cos_sigma) = Math.SinCos(sigma);
		(double sin_alpha, double cos_alpha) = Math.SinCos(alpha);
		(double sin_U1, double cos_U1) = Math.SinCos(U1);

		double phi2 = Math.Atan2(sin_U1 * cos_sigma + cos_U1 * sin_sigma * cos(alpha1),
								 (1 - f) * Math.Sqrt(square(sin_alpha) + square(sin_U1 * sin_sigma - cos_U1 * cos_sigma * cos(alpha1))));
		double lambda = Math.Atan2(sin_sigma * sin(alpha1),
								   cos_U1 * cos_sigma - sin_U1 * sin_sigma * cos(alpha1));

		double C = (f / 16) * square(cos_alpha) * (4 + f * (4 - 3 * square(cos_alpha)));
		double L = lambda - (1 - C) * f * sin_alpha * (sigma + C * sin_sigma * (cos(2 * twoSigmaM) + C * cos_sigma * (-1 + 2 * square(cos(2 * twoSigmaM)))));

		double L2 = L + L1;

		phi2 *= RAD_TO_DEG;
		L2 *= RAD_TO_DEG;

		return new(0, phi2, L2, System.Collections.Frozen.FrozenDictionary<string, string>.Empty);
	}

	[DebuggerStepThrough]
	public static (double bearing, double distance) GetBearingDistance(this Node origin, Node other)
	{
		if (origin == other)
			return (double.NaN, 0);

		// Inverse Vincenty
		const double a = 3443.918;
		const double b = 3432.3716599595;
		const double f = 1 / 298.257223563;
		const double DEG_TO_RAD = Math.Tau / 360;
		const double RAD_TO_DEG = 360 / Math.Tau;
		static double square(double x) => x * x;
		static double cos(double x) => Math.Cos(x);
		static double sin(double x) => Math.Sin(x);

		double phi1 = origin.Latitude * DEG_TO_RAD,
			   L1 = origin.Longitude * DEG_TO_RAD,
			   phi2 = other.Latitude * DEG_TO_RAD,
			   L2 = other.Longitude * DEG_TO_RAD;

		double U1 = Math.Atan((1 - f) * Math.Tan(phi1)),
			   U2 = Math.Atan((1 - f) * Math.Tan(phi2)),
			   L = L2 - L1;

		double lambda = L, oldLambda;

		(double sin_U1, double cos_U1) = Math.SinCos(U1);
		(double sin_U2, double cos_U2) = Math.SinCos(U2);

		double cos_2_alpha = 0, sin_sigma = 0, cos_sigma = 0, sigma = 0, cos_2_sigmaM = 0;

		for (int iterCntr = 0; iterCntr < 100; ++iterCntr)
		{
			sin_sigma = Math.Sqrt(
					square(
						cos_U2 * sin(lambda)
					) + square(
						(cos_U1 * sin_U2) - (sin_U1 * cos_U2 * cos(lambda))
					)
				);

			cos_sigma = sin_U1 * sin_U2 + cos_U1 * cos_U2 * cos(lambda);

			sigma = Math.Atan2(sin_sigma, cos_sigma);

			double sin_alpha = (cos_U1 * cos_U2 * sin(lambda)) / sin_sigma;

			cos_2_alpha = 1 - square(sin_alpha);

			cos_2_sigmaM = cos_sigma - (2 * sin_U1 * sin_U2 / cos_2_alpha);

			double C = f / 16 * cos_2_alpha * (4 + f * (4 - 3 * cos_2_alpha));

			oldLambda = lambda;
			lambda = L + (1 - C) * f * sin_alpha * (sigma + C * sin_sigma * (cos_2_sigmaM) + C * cos_sigma * (-1 + 2 * square(cos_2_sigmaM)));

			if (Math.Abs(lambda - oldLambda) > 1.0E-9)
				break;
		}

		double u2 = cos_2_alpha * ((square(a) - square(b)) / square(b));

		double A = 1 + u2 / 16384 * (4096 + u2 * (-768 + u2 * (320 - 175 * u2))),
			   B = u2 / 1024 * (256 + u2 * (-128 + u2 * (74 - 47 * u2)));

		double delta_sigma = B * sin_sigma * (cos_2_sigmaM + 1 / 4 * B * (cos_sigma * (-1 + 2 * square(cos_2_sigmaM)) - B / 6 * cos_2_sigmaM * (-3 + 4 * square(sin_sigma)) * (-3 + 4 * square(cos_2_sigmaM))));

		double s = b * A * (sigma - delta_sigma);
		double alpha_1 = Math.Atan2(
				cos_U2 * sin(lambda),
				cos_U1 * sin_U2 - sin_U1 * cos_U2 * cos(lambda)
			);

		if (double.IsNaN(s))
			return (double.NaN, 0);
		else if (double.IsNaN(alpha_1))
			return (double.NaN, s);

		return (alpha_1 * RAD_TO_DEG, s);
	}

	public static Way BreakAntimeridian(this Way source)
	{
		static double lerp(double a, double b, double t) => a + (b - a) * t;

		List<Node> nodes = [];
		Node last = source.Nodes[0];

		foreach (Node coord in source.Nodes)
		{
			if (coord.Longitude < -90 && last.Longitude > 90 || coord.Longitude > 90 && last.Longitude < -90)
			{
				double thisDist = 180 - Math.Abs(coord.Latitude);
				double ratio = thisDist / (thisDist + (180 - Math.Abs(last.Latitude)));

				Node left = new(0, lerp(last.Latitude, coord.Latitude, ratio), 180, FrozenDictionary<string, string>.Empty),
					 right = new(0, lerp(last.Latitude, coord.Latitude, ratio), -180, FrozenDictionary<string, string>.Empty);

				// Break up points at the antimeridian.
				if (last.Longitude > 0)
				{
					nodes.Add(left);
					nodes.Add(right);
				}
				else
				{
					nodes.Add(right);
					nodes.Add(left);
				}

				nodes.Add(coord);
			}
			else
				nodes.Add(coord);

			last = coord;
		}

		return new(source.Id, [..nodes], source.Tags);
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