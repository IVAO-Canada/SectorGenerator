using System.Collections.Frozen;

using WSleeman.Osm;

using static SectorGenerator.Helpers;

namespace SectorGenerator;

internal class Osm(OsmData data)
{
	private readonly OsmData _aerodata = data;

	public FrozenDictionary<long, Node> Nodes => _aerodata.Nodes;

	public FrozenDictionary<long, Way> Ways => _aerodata.Ways;

	public FrozenDictionary<long, Relation> Relations => _aerodata.Relations;

	public static async Task<Osm> Load() =>
		new((await Overpass.FromQueryAsync($$"""
		[out:json][timeout:{{(int)TimeSpan.FromMinutes(7).TotalSeconds}}];
		(area["ISO3166-1:alpha3"="USA"]; area["ISO3166-1:alpha3"="BHS"];)->.searchArea;
		(
			nwr[aeroway=aerodrome][icao][!abandoned](area.searchArea);
			way[aeroway=holding_position](area.searchArea);
			nw[aeroway=parking_position](area.searchArea);
			wr[aeroway=apron](area.searchArea);
			wr[aeroway=terminal](area.searchArea);
			wr[aeroway=hangar](area.searchArea);
			way[aeroway=taxilane](area.searchArea);
			way[aeroway=taxiway](area.searchArea);
			wr[aeroway=helipad](area.searchArea);
			way[aeroway=runway](area.searchArea);
		)->.baseData;
		(
			.baseData;
			>>;
		);
		out;
		""", 8)));

	public Osm InRegion((double Latitude, double Longitude)[] boundingRegion)
	{
		bool checkNode(Node n) => IsInPolygon(boundingRegion, (n.Latitude, n.Longitude));
		bool checkItem(OsmItem i) => i switch {
			Node n => checkNode(n),
			Way w => w.Nodes.Any(checkNode),
			Relation r => r.Members.Any(checkItem),
			_ => throw new NotImplementedException()
		};

		return new(_aerodata.Filter(checkItem));
	}

	public Osm InRegion(Way boundingRegion)
	{
		bool checkNode(Node n) => IsInPolygon(boundingRegion, (n.Latitude, n.Longitude));
		bool checkItem(OsmItem i) => i switch {
			Node n => checkNode(n),
			Way w => w.Nodes.Any(checkNode),
			Relation r => r.Members.Any(checkItem),
			_ => throw new NotImplementedException()
		};

		return new(_aerodata.Filter(checkItem));
	}

	public Osm GetFiltered(Func<OsmItem, bool> filter) => new(_aerodata.Filter(filter));

	/// <summary>Allocates each geo to the first boundary in which it occurs.</summary>
	/// <remarks>Will only put each item in one boundary, even if it is in multiple.</remarks>
	public IDictionary<T, Osm> Group<T>(IDictionary<T, Way> boundaries, double? maxDistance = null) where T : class
	{
		static T? checkNode(Node n, IEnumerable<KeyValuePair<T, Way>> boundaries) =>
			boundaries.Cast<KeyValuePair<T, Way>?>().FirstOrDefault(kvp => IsInPolygon(kvp!.Value.Value, (n.Latitude, n.Longitude)))?.Key;

		T? checkItem(OsmItem i)
		{
			switch (i)
			{
				case Node n:
					return checkNode(n, boundaries);

				case Way w:
					if (w.Nodes.Length == 0)
						return null;

					Node referenceNode = w.Nodes[0];

					double? mdSquared = maxDistance is double md ? md * md : null;

					var localBoundaries =
						mdSquared is double mds
						? boundaries.Where(b => b.Value.Nodes.Length > 0 && b.Value.Nodes[0].ApproxDistanceSquaredTo(referenceNode) < mds)
						: boundaries;

					return w.Nodes.Select(n => checkNode(n, localBoundaries)).FirstOrDefault(i => i is not null);

				case Relation r:
					return r.Members.Select(checkItem).FirstOrDefault(i => i is not null);

				default: throw new NotImplementedException();
			}
			;
		}

		Dictionary<T, FrozenDictionary<long, Node>> nodeGroups = _aerodata.Nodes.AsParallel().AsUnordered().GroupBy(kvp => checkItem(kvp.Value)).Where(g => g.Key is not null).ToDictionary(g => g.Key!, g => g.ToFrozenDictionary());
		Dictionary<T, FrozenDictionary<long, Way>> wayGroups = _aerodata.Ways.AsParallel().AsUnordered()
			.GroupBy(kvp => checkItem(kvp.Value))
			.Where(kvp => kvp.Key is not null)
			.ToDictionary(g => g.Key!, g => g.ToFrozenDictionary());
		Dictionary<T, FrozenDictionary<long, Relation>> relationGroups = _aerodata.Relations.AsParallel().AsUnordered().GroupBy(kvp => checkItem(kvp.Value)).Where(kvp => kvp.Key is not null).ToDictionary(g => g.Key!, g => g.ToFrozenDictionary());

		var keys = nodeGroups.Keys.Union(wayGroups.Keys).Union(relationGroups.Keys);

		return keys.ToDictionary(k => k, k =>
			new Osm(new Overpass(
				nodeGroups.TryGetValue(k, out var ng) ? ng : FrozenDictionary<long, Node>.Empty,
				wayGroups.TryGetValue(k, out var wg) ? wg : FrozenDictionary<long, Way>.Empty,
				relationGroups.TryGetValue(k, out var rg) ? rg : FrozenDictionary<long, Relation>.Empty
			))
		);
	}
}
