using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.IO.Compression;
using System.Xml;

using static SectorGenerator.Helpers;

namespace SectorGenerator;
internal partial class Mrva : IDisposable
{
	private readonly string _fileDir;

	readonly ConcurrentDictionary<string, MrvaSegment[]> _mrvaBlobs = [];
	readonly HttpClient _http = new();
	readonly ConcurrentDictionary<string, string> _fileCache = [];

	/// <summary>A dictionary mapping place names (usually TRACONs) to a set of MRVA segments.</summary>
	public FrozenDictionary<string, MrvaSegment[]> Volumes => _volumes;

	FrozenDictionary<string, MrvaSegment[]> _volumes = FrozenDictionary<string, MrvaSegment[]>.Empty;

	public static async Task<Mrva> LoadMrvasAsync()
	{
		Mrva retval = new(Path.Combine(Path.GetTempPath(), "mrvas"));
		await retval.GenerateMrvasAsync();
		return retval;
	}

	public Mrva(string fileDir) => _fileDir = fileDir;

	private async Task<Dictionary<string, XmlDocument>> GetMrvaXmlDocs()
	{
		Dictionary<string, XmlDocument> retval = [];

		if (!Directory.Exists(_fileDir))
			Directory.CreateDirectory(_fileDir);

		using (Stream githubStream = await _http.GetStreamAsync("https://github.com/dark/faa-mva-kml/archive/refs/heads/master.zip"))
		{
			ZipFile.ExtractToDirectory(githubStream, _fileDir, true);
		}

		foreach (string filePath in Directory.EnumerateFiles(_fileDir).Where(static fp => fp.Contains("FUS3")))
		{
			XmlDocument xmlDoc = new();
			if (!_fileCache.TryGetValue(filePath, out string? mrvaXmlData))
			{
				int failcount = 0;
				while (mrvaXmlData is null && failcount < 5)
					mrvaXmlData = File.ReadAllText(filePath);

				if (failcount >= 5)
					continue;

				_fileCache[filePath] = mrvaXmlData!;
			}

			xmlDoc.LoadXml(mrvaXmlData!);
			retval[filePath] = xmlDoc;
		}

		return retval;
	}

	private async Task GenerateMrvasAsync()
	{
		if (_volumes.Count is not 0)
			return;

		if (_mrvaBlobs.IsEmpty)
			foreach (var (xmlUrl, xmlDoc) in await GetMrvaXmlDocs())
			{
				if (xmlDoc["ns8:AIXMBasicMessage"] is not XmlNode rootNode)
					continue;

				List<MrvaSegment> segments = [];
				string? nameParts =
					rootNode["ns1:description"] is XmlNode descNode
					? descNode.InnerText.Split("_MVA")[0] : null;

				foreach (XmlNode airspace in rootNode.ChildNodes.Cast<XmlNode>()
											.Where(static n => n.Name is "ns8:hasMember" && n["ns3:Airspace"] is not null)
											.Select(n => n["ns3:Airspace"]!))
				{
					if (airspace["ns3:timeSlice"]?["ns3:AirspaceTimeSlice"] is not XmlNode timeSlice ||
						timeSlice["ns3:name"] is not XmlNode nameNode ||
						timeSlice["ns3:geometryComponent"]?["ns3:AirspaceGeometryComponent"]?["ns3:theAirspaceVolume"]?["ns3:AirspaceVolume"] is not XmlNode volume ||
						volume["ns3:minimumLimit"] is not XmlNode minimumLimitNode ||
						!int.TryParse(minimumLimitNode.InnerText, out int minLimit) ||
						volume["ns3:horizontalProjection"]?["ns3:Surface"]?["ns1:patches"]?["ns1:PolygonPatch"]?["ns1:exterior"]?["ns1:LinearRing"]?["ns1:posList"] is not XmlNode posListNode)
						continue;

					string name = nameNode.InnerText;
					nameParts ??= 'K' + name[..3];
					double[] posListElems = [.. posListNode.InnerText.Split().Select(double.Parse)];
					(double, double)[] boundaryPoints = [..Enumerable.Range(0, posListElems.Length / 2).Select(idx =>
					(posListElems[idx * 2 + 1], posListElems[idx * 2])
				)];

					segments.Add(new(name, minLimit, boundaryPoints));
				}

				if (nameParts is null)
					continue;

				_mrvaBlobs.TryAdd(nameParts, [.. segments]);
			}

		_volumes = _mrvaBlobs.ToFrozenDictionary();
	}

	public (double Latitude, double Longitude) PlaceLabel(MrvaSegment segment) => PlaceLabel(segment.BoundaryPoints, _volumes.Values.SelectMany(v => v.Select(s => s.BoundaryPoints)));

	public static (double Latitude, double Longitude) PlaceLabel((double Latitude, double Longitude)[] boundary, IEnumerable<(double Latitude, double Longitude)[]> otherBoundaries)
	{
		var cp = (boundary.Average(bp => bp.Latitude), boundary.Average(bp => bp.Longitude));
		double maxDist = Math.Sqrt(boundary.Max(bp => FastSquaredDistanceTo(cp, bp)));

		(double Lat, double Lon)[][] overlapping = [.. otherBoundaries.Where(ob => !ob.SequenceEqual(boundary) && AreaContainsArea(boundary, ob))];

		if (IsInPolygon(boundary, cp) && !overlapping.Any(o => IsInPolygon(o, cp)))
			return cp;

		(double, double) stabilise((double Lat, double Lon) point)
		{
			double pointSqDist = FastSquaredDistanceTo(cp, point);
			var furtherPoints = boundary.Where(bp => FastSquaredDistanceTo(cp, bp) > pointSqDist).ToArray();

			if (furtherPoints.Length == 0)
				return point;

			var bp = furtherPoints.MinBy(bp => FastSquaredDistanceTo(point, bp));
			double startDist = Math.Sqrt(FastSquaredDistanceTo(point, bp)) / 120;
			double radAng = Math.Atan2(bp.Latitude - point.Lat, bp.Longitude - point.Lon);
			var sincos = Math.SinCos(radAng);

			for (double dist = startDist; dist > 0; dist -= startDist / 10)
			{
				var testPoint = (point.Lat + sincos.Sin * dist, point.Lon + sincos.Cos * dist);
				if (IsInPolygon(boundary, testPoint) && !overlapping.Any(o => IsInPolygon(o, testPoint)))
					return testPoint;
			}

			return point;
		}

		for (int dist = 1; dist < maxDist * 10; ++dist)
		{
			for (double angle = 225; angle >= 0; angle -= 45)
			{
				(double Lat, double Lon) testPoint = FixRadialDistance(cp, angle, dist / 10.0);
				if (IsInPolygon(boundary, testPoint) && !overlapping.Any(o => IsInPolygon(o, testPoint)))
					return stabilise(testPoint);
			}

			for (double angle = 315; angle >= 270; angle -= 45)
			{
				(double Lat, double Lon) testPoint = FixRadialDistance(cp, angle, dist / 10.0);
				if (IsInPolygon(boundary, testPoint) && !overlapping.Any(o => IsInPolygon(o, testPoint)))
					return stabilise(testPoint);
			}
		}

		return cp;
	}

	static (double Latitude, double Longitude) FixRadialDistance((double Lat, double Lon) origin, double radial, double distance)
	{
		const double DEG_TO_RAD = Math.Tau / 360;
		double radialRad = radial * DEG_TO_RAD,
			   degDist = distance / 60;

		var sincos = Math.SinCos(radialRad);

		return (origin.Lat + degDist * sincos.Cos, origin.Lon + degDist * sincos.Sin / Math.Cos(origin.Lat * DEG_TO_RAD));
	}

	static double FastSquaredDistanceTo((double Lat, double Lon) from, (double Lat, double Lon) to)
	{
		double dLat = (to.Lat - from.Lat) * 60,
			   dLon = (to.Lon - from.Lon) * 60 * Math.Cos(from.Lat * Math.Tau / 360);

		return dLat * dLat + dLon * dLon;
	}

	public static bool AreaContainsArea((double, double)[] area1, (double, double)[] area2) =>
		area2.Any(p => IsInPolygon(area1, p));

	public void Dispose()
	{
		if (Directory.Exists(_fileDir))
			Directory.Delete(_fileDir, true);

		_http.Dispose();
	}

	public record MrvaSegment(string Name, int MinimumAltitude, (double Latitude, double Longitude)[] BoundaryPoints) { }
}
