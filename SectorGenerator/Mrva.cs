using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Text.RegularExpressions;
using System.Xml;

using static SectorGenerator.Helpers;

namespace SectorGenerator;
internal partial class Mrva
{
	const string FAA_MRVA_LISTING = @"https://aeronav.faa.gov/MVA_Charts/aixm/";
	const string FAA_MRVA_ROOT = @"https://aeronav.faa.gov";

	static readonly ConcurrentDictionary<string, MrvaSegment[]> _mrvaBlobs = [];
	static readonly HttpClient _http = new();
	static readonly ConcurrentDictionary<string, string> _fileCache = [];
	static string? _directoryListing = null;

	public FrozenDictionary<string, MrvaSegment[]> Volumes => _volumes;

	FrozenDictionary<string, MrvaSegment[]> _volumes = FrozenDictionary<string, MrvaSegment[]>.Empty;

	public Mrva((double, double)[] boundary)
	{
		Task t = Task.Run(async () => await GenerateMrvasAsync(boundary));

		DateTimeOffset startTime = DateTimeOffset.UtcNow;
		while (!t.IsCompleted && (DateTimeOffset.UtcNow - startTime).TotalMinutes < 1)
			Task.Delay(100).Wait();
	}

	private async Task<Dictionary<string, XmlDocument>> GetMrvaXmlDocs()
	{
		Dictionary<string, XmlDocument> retval = [];
		_directoryListing ??= await _http.GetStringAsync(FAA_MRVA_LISTING);

		foreach (string xmlUrl in Fus3Url().Matches(_directoryListing).Select(m => m.Groups["url"].Value))
		{
			XmlDocument xmlDoc = new();
			if (!_fileCache.TryGetValue(FAA_MRVA_ROOT + xmlUrl, out string? mrvaXmlData))
			{
				int failcount = 0;
				while (mrvaXmlData is null && failcount < 5)
				{
					try
					{
						mrvaXmlData = await _http.GetStringAsync(FAA_MRVA_ROOT + xmlUrl);
					}
					catch (HttpRequestException) { failcount++; }
				}
				if (failcount >= 5)
					continue;

				_fileCache[FAA_MRVA_ROOT + xmlUrl] = mrvaXmlData!;
			}

			xmlDoc.LoadXml(mrvaXmlData!);
			retval[xmlUrl] = xmlDoc;
		}

		return retval;
	}

	private async Task GenerateMrvasAsync((double, double)[] boundary)
	{
		if (_mrvaBlobs.IsEmpty)
		{
			foreach (var (xmlUrl, xmlDoc) in await GetMrvaXmlDocs())
			{
				if (xmlDoc["ns8:AIXMBasicMessage"] is not XmlNode rootNode)
					continue;

				List<MrvaSegment> segments = [];
				string? nameParts =
					rootNode["ns1:description"] is XmlNode descNode
					? descNode.InnerText.Split("_MVA")[0] : null;

				foreach (XmlNode airspace in rootNode.ChildNodes.Cast<XmlNode>()
											.Where((XmlNode n) => n.Name == "ns8:hasMember" && n["ns3:Airspace"] is not null)
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
		}

		_volumes = _mrvaBlobs
		.Where(blob => boundary.Length > 0 && blob.Value.Any(seg => seg.BoundaryPoints.Any(p => IsInPolygon(boundary, p))))
		.ToFrozenDictionary();
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

	[GeneratedRegex("<a href=\"(?<url>[^\"]+FUS3[^\"]+xml)\">", RegexOptions.ExplicitCapture | RegexOptions.Compiled | RegexOptions.IgnoreCase)]
	private partial Regex Fus3Url();

	public record MrvaSegment(string Name, int MinimumAltitude, (double Latitude, double Longitude)[] BoundaryPoints) { }
}
