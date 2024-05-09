using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Text.RegularExpressions;
using System.Xml;

using WSleeman.Osm;

using static SectorGenerator.Helpers;

namespace SectorGenerator;
internal partial class Mrva
{
	const string FAA_MRVA_LISTING = @"https://aeronav.faa.gov/MVA_Charts/aixm/";
	const string FAA_MRVA_ROOT = @"https://aeronav.faa.gov";

	static ConcurrentDictionary<string, MrvaSegment[]> _mrvaBlobs = [];
	static readonly HttpClient _http = new();

	public FrozenDictionary<string, MrvaSegment[]> Volumes { get; private set; } = FrozenDictionary<string, MrvaSegment[]>.Empty;

	public Mrva((double, double)[] boundary)
	{
		Task t = Task.Run(GenerateMrvas);

		while (!t.IsCompleted)
			Thread.Sleep(100);

		Volumes = _mrvaBlobs
			.Where(blob => blob.Value.Any(seg => seg.BoundaryPoints.Any(p => IsInPolygon(boundary, p))))
			.ToFrozenDictionary();
	}

	private async Task GenerateMrvas()
	{
		if (_mrvaBlobs.Count > 0)
			return;

		string dirListing = await _http.GetStringAsync(FAA_MRVA_LISTING);

		Parallel.ForEach(Fus3Url().Matches(dirListing).Select(m => m.Groups["url"].Value), xmlUrl =>
		{
			XmlDocument xmlDoc = new();
			xmlDoc.Load(FAA_MRVA_ROOT + xmlUrl);
			if (xmlDoc["ns8:AIXMBasicMessage"] is not XmlNode rootNode)
				return;

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
				return;

			_mrvaBlobs.TryAdd(nameParts, [.. segments]);
		});
	}

	[GeneratedRegex("<a href=\"(?<url>[^\"]+FUS3[^\"]+xml)\">", RegexOptions.ExplicitCapture | RegexOptions.Compiled | RegexOptions.IgnoreCase)]
	private partial Regex Fus3Url();

	public record MrvaSegment(string Name, int MinimumAltitude, (double Latitude, double Longitude)[] BoundaryPoints) { }
}
