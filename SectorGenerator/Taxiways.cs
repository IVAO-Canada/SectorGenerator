using WSleeman.Osm;

namespace SectorGenerator;

internal class Taxiways(string airport, Osm taxiways)
{
	private readonly Osm _osm = taxiways;

	public string Labels => string.Join(
		"\r\n",
		_osm.WaysAndBoundaries().Where(w => w.Tags.ContainsKey("ref")).SelectMany(w =>
			w.Nodes[..^1].Zip(w.Nodes[1..]).Where(ns => ns.First.DistanceTo(ns.Second) is double d && d > 0.02).Select(p => (w.Tags["ref"], p)) // Eliminate segments less than 120ft long.
		).Select(pair => (Label: pair.Item1, Lat: (pair.p.First.Latitude + pair.p.Second.Latitude) / 2, Lon: (pair.p.First.Longitude + pair.p.Second.Longitude) / 2))
		.Select(l => $"{l.Label};{airport};{l.Lat:00.0######};{l.Lon:000.0######};")
	);

	public string Centerlines => string.Join(
		"\r\n",
		_osm.WaysAndBoundaries().Select(w => string.Join(
			"\r\n",
			w.Nodes[..^1].Zip(w.Nodes[1..]).Select(n => $"{n.First.Latitude:00.0######};{n.First.Longitude:000.0######};{n.Second.Latitude:00.0######};{n.Second.Longitude:000.0######};TAXI_CENTER;")
		))
	);

	public Way[] BoundingBoxes => [.. _osm.WaysAndBoundaries().Select(w => w.Inflate(0.0001))];
}
