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

	static ConcurrentDictionary<string, MrvaSegment[]> _mrvaBlobs = [];
	static readonly HttpClient _http = new();
	static string? _directoryListing = null;
	static ConcurrentDictionary<string, string> _fileCache = [];

	public Task<FrozenDictionary<string, MrvaSegment[]>> Volumes => Task.Run<FrozenDictionary<string, MrvaSegment[]>>(async () =>
	{
		while (_loadTask is null)
			await Task.Delay(100);

		if (!_loadTask.IsCompleted)
			await _loadTask;

		return _volumes;
	});

	FrozenDictionary<string, MrvaSegment[]> _volumes = FrozenDictionary<string, MrvaSegment[]>.Empty;
	private Task? _loadTask;

	public Mrva((double, double)[] boundary)
	{
		_loadTask = Task.Run(() => GenerateMrvas(boundary));
	}

	private async Task GenerateMrvas((double, double)[] boundary)
	{
		if (_mrvaBlobs.Count > 0)
			return;

		_directoryListing ??= await _http.GetStringAsync(FAA_MRVA_LISTING);

		Parallel.ForEach(Fus3Url().Matches(_directoryListing).Select(m => m.Groups["url"].Value), async xmlUrl =>
		{
			XmlDocument xmlDoc = new();
			if (!_fileCache.TryGetValue(FAA_MRVA_ROOT + xmlUrl, out string? mrvaXmlData))
			{
				mrvaXmlData = await _http.GetStringAsync(FAA_MRVA_ROOT + xmlUrl);
				_fileCache[FAA_MRVA_ROOT + xmlUrl] = mrvaXmlData;
			}

			xmlDoc.LoadXml(mrvaXmlData);
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

		_volumes = _mrvaBlobs
		.Where(blob => blob.Value.Any(seg => seg.BoundaryPoints.Any(p => IsInPolygon(boundary, p))))
		.ToFrozenDictionary();
	}

	[GeneratedRegex("<a href=\"(?<url>[^\"]+FUS3[^\"]+xml)\">", RegexOptions.ExplicitCapture | RegexOptions.Compiled | RegexOptions.IgnoreCase)]
	private partial Regex Fus3Url();

	public record MrvaSegment(string Name, int MinimumAltitude, (double Latitude, double Longitude)[] BoundaryPoints) { }
}
