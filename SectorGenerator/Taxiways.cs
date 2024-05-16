using WSleeman.Osm;

namespace SectorGenerator;

internal class Taxiways(string airport, Osm taxiways)
{
	const double LABEL_SPACING = 0.25;

	private readonly Osm _osm = taxiways;

	public string Labels => string.Join("\r\n",
		_osm.WaysAndBoundaries().Where(w => w.Tags.ContainsKey("ref")).SelectMany(SpacePointsOnWay)
		.Select(l => $"{l.Label};{airport};{l.Latitude:00.0######};{l.Longitude:000.0######};")
	);

	public string Centerlines => string.Join(
		"\r\n",
		_osm.WaysAndBoundaries().Select(w => string.Join(
			"\r\n",
			w.Nodes[..^1].Zip(w.Nodes[1..]).Select(n => $"{n.First.Latitude:00.0######};{n.First.Longitude:000.0######};{n.Second.Latitude:00.0######};{n.Second.Longitude:000.0######};TAXI_CENTER;")
		))
	);

	public Way[] BoundingBoxes => [.. _osm.WaysAndBoundaries().Select(w => w.Inflate(0.0001))];

	internal static (string Label, double Latitude, double Longitude)[] SpacePointsOnWay(Way w)
	{
		if (w.Nodes.Length < 2)
			return [];

		string name = w["ref"] ?? "";
		List<Node> points = [];

		Node last = w.Nodes[0];

		foreach ((Node prev, Node next) in w.Nodes[..^1].Zip(w.Nodes[1..]))
		{
			var (bearing, dist) = prev.GetBearingDistance(next);
			Node candidate = prev.FixRadialDistance(bearing, dist / 2);
			if (candidate.ApproxDistanceSquaredTo(last) > LABEL_SPACING * LABEL_SPACING)
			{
				points.Add(candidate);
				last = candidate;
			}
		}

		if (last == w.Nodes[0])
			// Short way. Grab the middle and go with it.
			return [(name, w.Nodes[w.Nodes.Length / 2].Latitude, w.Nodes[w.Nodes.Length / 2].Longitude)];
		else if (last.ApproxDistanceSquaredTo(w.Nodes[^1]) > LABEL_SPACING * LABEL_SPACING)
		{
			// Weird long stretch at the end. Put a point in to not look weird.
			var (bearing, dist) = w.Nodes[^1].GetBearingDistance(w.Nodes[^2]);
			points.Add(
				dist >= LABEL_SPACING
				? w.Nodes[^1]
				: w.Nodes[^1].FixRadialDistance(bearing, LABEL_SPACING)
			);
		}

		return [.. points.Select(p => (name, p.Latitude, p.Longitude))];
	}
}
