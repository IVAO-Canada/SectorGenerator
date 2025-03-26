using WSleeman.Osm;

namespace SectorGenerator;

internal class Taxiways(string airport, Osm taxiways)
{
	const double LABEL_SPACING = 0.25;

	private readonly Osm _osm = taxiways;

	public string Labels
	{
		get
		{
			const float SPACING_SHORT = 0.0025f, SPACING_LONG = 0.005f;

			Way[] taxiways = [.. _osm.WaysAndBoundaries().Where(w => w.Tags.ContainsKey("ref"))];
			HashSet<(string Label, double Latitude, double Longitude)> labels = [];
			HashSet<string> neededLabels = [], placedLabels = [];

			foreach (Way taxiway in taxiways)
			{
				string label = taxiway["ref"]!;
				neededLabels.Add(label);

				if (taxiway.Nodes.Length <= 1)
					continue;

				float spacing = label.Any(char.IsDigit) ? SPACING_SHORT : SPACING_LONG;

				float distSinceLast = spacing;
				float totalDistance = 0;

				var (iLastX, iLastY) = (taxiway.Nodes[0].Longitude, taxiway.Nodes[0].Latitude);

				foreach (var node in taxiway.Nodes[1..])
				{
					var (x, y) = (node.Longitude, node.Latitude);
					float dx = (float)(x - iLastX), dy = (float)(y - iLastY);
					totalDistance += MathF.Sqrt(dx * dx + dy * dy);
					(iLastX, iLastY) = (x, y);
				}

				if (totalDistance * 2 < spacing)
					continue;

				distSinceLast = (totalDistance % spacing) / 2;

				var (lastX, lastY) = (taxiway.Nodes[0].Longitude, taxiway.Nodes[0].Latitude);
				int labelsPlaced = 0;

				foreach (var node in taxiway.Nodes[1..])
				{
					var (x, y) = (node.Longitude, node.Latitude);
					while (lastX != x || lastY != y)
					{
						float dx = (float)(x - lastX), dy = (float)(y - lastY);
						float distRemaining = MathF.Sqrt(dx * dx + dy * dy);

						float stepLength = Math.Min(distRemaining, spacing - distSinceLast);
						float norm = stepLength / distRemaining;
						(lastX, lastY) = (lastX + dx * norm, lastY + dy * norm);
						distSinceLast += stepLength;

						if (distSinceLast >= spacing)
						{
							labels.Add((label, y, x));
							++labelsPlaced;
							distSinceLast = 0;
						}
					}
				}

				if (labelsPlaced > 0)
					placedLabels.Add(label);
			}

			foreach (var label in neededLabels.Except(placedLabels))
			{
				// Find the taxiways that were too short and give them their own labels in the middle so we didn't miss any letters.
				Node[]? nodes = taxiways.Where(tw => tw["ref"] == label).MaxBy(tw => tw.Nodes.Length)?.Nodes;

				if (nodes is null || nodes.Length < 1)
					continue;

				Node median = nodes[nodes.Length / 2];
				labels.Add((label, median.Latitude, median.Longitude));
			}

			return string.Join("\r\n",
				labels.Select(l => $"{l.Label};{airport};{l.Latitude:00.0######};{l.Longitude:000.0######};")
			);
		}
	}

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
