using System.Text;
using System.Text.Json.Nodes;

using CIFPReader;

using static CIFPReader.ControlledAirspace;
using static SectorGenerator.Helpers;

namespace SectorGenerator;

internal class CifpAirspaceDrawing(IEnumerable<Airspace> cifpAirspaces)
{
	private readonly Airspace[] _airspaces = [.. cifpAirspaces];
	private readonly (AirspaceClass AsClass, string Label, (double Latitude, double Longitude)[] Region)[] _linearRegions = [.. cifpAirspaces.SelectMany(LinearizeAirspace)];
	private readonly (AirspaceClass AsClass, (double Latitude, double Longitude) From, (double Latitude, double Longitude) To)[] _segmented = [.. cifpAirspaces.SelectMany(SegmentAirspace)];

	private static (AirspaceClass AsClass, (double Latitude, double Longitude) From, (double Latitude, double Longitude) To)[] SegmentAirspace(Airspace asp)
	{
		List<(AirspaceClass AsClass, (double Latitude, double Longitude) From, (double Latitude, double Longitude) To)> segments = [];

		foreach (var i in asp.Regions)
		{
			var (boundaries, asClass, altitudes) = i;
			string floorLabel = altitudes.Floor switch {
				AltitudeMSL msl => (msl.Feet / 100).ToString("000"),
				null => "SFC",
				AltitudeAGL sfc when sfc.Feet == 0 => "SFC",
				AltitudeAGL agl when agl.GroundElevation is null => $"SFC + {agl.Feet / 100:000}",
				AltitudeAGL conv => (conv.ToMSL().Feet / 100).ToString("000"),
				_ => throw new NotImplementedException()
			};

			string altitudeBlock = $"{(altitudes.Ceiling?.Feet / 100)?.ToString("000") ?? "UNL"}\n{floorLabel}";

			Route drawRoute = new(altitudeBlock);
			Coordinate? first = null, arcVertex = null, arcOrigin = null;
			bool clockwise = false;


			foreach (var seg in boundaries)
			{
				Coordinate next = seg switch {
					BoundaryArc a => a.ArcVertex,
					BoundaryLine l => l.Vertex,
					BoundaryEuclidean e => e.Vertex,
					BoundaryCircle c => c.Centerpoint,
					_ => throw new NotImplementedException()
				};

				first ??= next;

				if (arcVertex is Coordinate vertex && arcOrigin is Coordinate origin)
					drawRoute.AddArc(vertex, next, origin, clockwise);
				else if (seg is BoundaryCircle c)
				{
					if (boundaries.Count() > 1)
						throw new ArgumentException("Made an airspace region with more than just a circle!");

					Coordinate top = c.Centerpoint.FixRadialDistance(new TrueCourse(000), c.Radius),
							  left = c.Centerpoint.FixRadialDistance(new TrueCourse(270), c.Radius),
							bottom = c.Centerpoint.FixRadialDistance(new TrueCourse(180), c.Radius),
							 right = c.Centerpoint.FixRadialDistance(new TrueCourse(090), c.Radius);

					clockwise = true;
					drawRoute.Add(top);
					drawRoute.AddArc(top, right, c.Centerpoint, clockwise);
					drawRoute.AddArc(right, bottom, c.Centerpoint, clockwise);
					drawRoute.AddArc(bottom, left, c.Centerpoint, clockwise);
					drawRoute.AddArc(left, top, c.Centerpoint, clockwise);
					break;
				}
				else
					drawRoute.Add(next);

				if (seg is BoundaryArc arc)
				{
					clockwise = arc.BoundaryVia.HasFlag(BoundaryViaType.ClockwiseArc);
					arcOrigin = arc.ArcOrigin.GetCoordinate();
					arcVertex = arc.Vertex;
				}
				else
				{
					arcOrigin = null;
					arcVertex = null;
				}

				if (seg.BoundaryVia.HasFlag(BoundaryViaType.ReturnToOrigin))
				{
					if (arcVertex is Coordinate retVertex && arcOrigin is Coordinate retOrigin)
						drawRoute.AddArc(retVertex, first.Value, retOrigin, clockwise);
					else
						drawRoute.Add(first.Value);
				}
			}

			segments.AddRange(drawRoute.ToSegments().Select(seg => (asClass, seg.From, seg.To)));
		}

		return [.. segments];
	}

	private static (AirspaceClass AsClass, string Label, (double Latitude, double Longitude)[] Region)[] LinearizeAirspace(Airspace asp)
	{
		List<(AirspaceClass AsClass, string Label, (double Latitude, double Longitude)[] Region)> segments = [];

		foreach (var i in asp.Regions)
		{
			var (boundaries, asClass, altitudes) = i;
			string floorLabel = altitudes.Floor switch {
				AltitudeMSL msl => (msl.Feet / 100).ToString("000"),
				null => "SFC",
				AltitudeAGL sfc when sfc.Feet == 0 => "SFC",
				AltitudeAGL agl when agl.GroundElevation is null => $"SFC + {agl.Feet / 100:000}",
				AltitudeAGL conv => (conv.ToMSL().Feet / 100).ToString("000"),
				_ => throw new NotImplementedException()
			};

			string altitudeBlock = $"{(altitudes.Ceiling?.Feet / 100)?.ToString("000") ?? "UNL"}\n{floorLabel}";

			Route drawRoute = new(altitudeBlock);
			Coordinate? first = null, arcVertex = null, arcOrigin = null;
			bool clockwise = false;


			foreach (var seg in boundaries)
			{
				Coordinate next = seg switch {
					BoundaryArc a => a.ArcVertex,
					BoundaryLine l => l.Vertex,
					BoundaryEuclidean e => e.Vertex,
					BoundaryCircle c => c.Centerpoint,
					_ => throw new NotImplementedException()
				};

				first ??= next;

				if (arcVertex is Coordinate vertex && arcOrigin is Coordinate origin)
					drawRoute.AddArc(vertex, next, origin, clockwise);
				else if (seg is BoundaryCircle c)
				{
					if (boundaries.Count() > 1)
						throw new ArgumentException("Made an airspace region with more than just a circle!");

					Coordinate top = c.Centerpoint.FixRadialDistance(new TrueCourse(000), c.Radius),
							  left = c.Centerpoint.FixRadialDistance(new TrueCourse(270), c.Radius),
							bottom = c.Centerpoint.FixRadialDistance(new TrueCourse(180), c.Radius),
							 right = c.Centerpoint.FixRadialDistance(new TrueCourse(090), c.Radius);

					clockwise = true;
					drawRoute.Add(top);
					drawRoute.AddArc(top, right, c.Centerpoint, clockwise);
					drawRoute.AddArc(right, bottom, c.Centerpoint, clockwise);
					drawRoute.AddArc(bottom, left, c.Centerpoint, clockwise);
					drawRoute.AddArc(left, top, c.Centerpoint, clockwise);
					break;
				}
				else
					drawRoute.Add(next);

				if (seg is BoundaryArc arc)
				{
					clockwise = arc.BoundaryVia.HasFlag(BoundaryViaType.ClockwiseArc);
					arcOrigin = arc.ArcOrigin.GetCoordinate();
					arcVertex = arc.Vertex;
				}
				else
				{
					arcOrigin = null;
					arcVertex = null;
				}

				if (seg.BoundaryVia.HasFlag(BoundaryViaType.ReturnToOrigin))
				{
					if (arcVertex is Coordinate retVertex && arcOrigin is Coordinate retOrigin)
						drawRoute.AddArc(retVertex, first.Value, retOrigin, clockwise);
					else
						drawRoute.Add(first.Value);
				}
			}

			segments.Add((asClass, altitudeBlock, drawRoute.ToSegments().SelectMany(seg => new[] { seg.From, seg.To }).Distinct().ToArray()));
		}

		return [.. segments];
	}

	public string ClassBPaths
	{
		get
		{
			StringBuilder sb = new();
			string last = "";

			foreach (var l in _segmented.Where(l => l.AsClass == AirspaceClass.B))
			{
				string fromLine = $"T;CLASS B;{l.From.Latitude:00.0#####};{l.From.Longitude:000.0#####};",
					   toLine = $"T;CLASS B;{l.To.Latitude:00.0#####};{l.To.Longitude:000.0#####};";

				if (last != fromLine)
				{
					sb.AppendLine("T;Dummy;N000.00.00.000;W000.00.00.000;");
					sb.AppendLine(fromLine);
				}

				sb.AppendLine(toLine);
				last = toLine;
			}

			return sb.ToString();
		}
	}

	public string ClassCPaths
	{
		get
		{
			StringBuilder sb = new();
			string last = "";

			foreach (var l in _segmented.Where(l => l.AsClass == AirspaceClass.C))
			{
				string fromLine = $"T;CLASS C;{l.From.Latitude:00.0#####};{l.From.Longitude:000.0#####};",
					   toLine = $"T;CLASS C;{l.To.Latitude:00.0#####};{l.To.Longitude:000.0#####};";

				if (last != fromLine)
				{
					sb.AppendLine("T;Dummy;N000.00.00.000;W000.00.00.000;");
					sb.AppendLine(fromLine);
				}

				sb.AppendLine(toLine);
				last = toLine;
			}

			return sb.ToString();
		}
	}

	public string ClassDPaths
	{
		get
		{
			StringBuilder sb = new();
			string last = "";

			foreach (var l in _segmented.Where(l => l.AsClass == AirspaceClass.D))
			{
				string fromLine = $"T;CLASS D;{l.From.Latitude:00.0#####};{l.From.Longitude:000.0#####};",
					   toLine = $"T;CLASS D;{l.To.Latitude:00.0#####};{l.To.Longitude:000.0#####};";

				if (last != fromLine)
				{
					sb.AppendLine("T;Dummy;N000.00.00.000;W000.00.00.000;");
					sb.AppendLine(fromLine);
				}

				sb.AppendLine(toLine);
				last = toLine;
			}

			return sb.ToString();
		}
	}

	public string ClassBLabels => string.Join("\r\n",
		_linearRegions.Where(r => r.AsClass == AirspaceClass.B).Select(r =>
		{
			var (labelLat, labelLon) = PlaceLabel(r.Region, [.. _linearRegions.Where(lr => lr.AsClass == AirspaceClass.B && lr != r).Select(lr => lr.Region)]);
			string[] lChunks = r.Label.ReplaceLineEndings("\n").Split('\n');
			return $"L;{lChunks[^1]}\\{lChunks[0]};{labelLat:00.0####};{labelLon:000.0####};8;";
		})
	);

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

	private class Route(string altitude) : IEnumerable<Route.RouteSegment>
	{
		public string Altitude { get; } = altitude;

		public IEnumerable<Coordinate> Points => _segments.Select(rs => rs.Point);
		public IEnumerable<(Coordinate Point, string? PointLabel)> LabelledPoints => _segments.Select(rs => (rs.Point, rs.PointLabel));

		readonly List<RouteSegment> _segments = [];

		public Route(string altitude, params (Coordinate start, string? pointLabel)[] points) : this(altitude)
		{
			_segments = new(points.Select(p => new StraightLineSegment(p.start, p.pointLabel)));
		}

		public Route(string altitude, params RouteSegment[] segments) : this(altitude) =>
			_segments = new(segments);

		public Route(string altitude, params Coordinate[] points) : this(altitude)
		{
			foreach (Coordinate i in points)
				_segments.Add(new StraightLineSegment(i, null));
		}

		public void Add(Coordinate point, string? pointLabel = null) => _segments.Add(new StraightLineSegment(point, pointLabel));
		public void AddArc(Coordinate controlPoint, Coordinate end, string? pointLabel = null) =>
			_segments.Add(new ArcSegment(controlPoint, end, pointLabel));

		public void AddArc(Coordinate from, Coordinate to, Coordinate origin, bool clockwise)
		{
			double getAngle(double degrees, double degrees2)
			{
				if (Math.Abs(degrees - degrees2) < 0.001)
					return 0;

				if (degrees > degrees2)
				{
					double num = degrees2 + 360 - degrees;
					double num2 = degrees - degrees2;
					if (num2 < num)
					{
						return -num2;
					}

					return num;
				}

				double num3 = degrees2 - degrees;
				double num4 = degrees + 360 - degrees2;

				return (num4 < num3) ? -num4 : num3;
			}

			double clampAngle(double angle)
			{
				while (angle < -360)
					angle += 360;

				while (angle > 360)
					angle -= 360;

				return angle;
			}

			void arcTo(Coordinate vertex, Coordinate next, Coordinate origin)
			{
#pragma warning disable IDE0042 // Variable declaration can be deconstructed
				var startData = origin.GetBearingDistance(vertex);
				var endData = origin.GetBearingDistance(next);
#pragma warning restore IDE0042 // Variable declaration can be deconstructed
				double startBearing = (double?)startData.bearing?.ToTrue()?.Degrees ?? (vertex.Latitude > origin.Latitude ? 0 : 180);
				double endBearing = (double?)endData.bearing?.ToTrue()?.Degrees ?? (next.Latitude > origin.Latitude ? 0 : 180);

				double guessBearing = getAngle(startBearing, endBearing);
				double realBearing = clampAngle(guessBearing / 2 + startBearing);

				if ((clockwise && guessBearing < 0) || (!clockwise && guessBearing > 0))
					realBearing = clampAngle(realBearing + 180);

				var midPoint = origin.FixRadialDistance(new TrueCourse((decimal)realBearing), startData.distance);

				if (midPoint != vertex && midPoint != next && Math.Abs(clampAngle(getAngle(startBearing, realBearing) + getAngle(realBearing, endBearing))) > 15)
				{
					arcTo(vertex, midPoint, origin);
					arcTo(midPoint, next, origin);
					return;
				}

				var controlLat = 2 * midPoint.Latitude - vertex.Latitude / 2 - next.Latitude / 2;
				var controlLon = 2 * midPoint.Longitude - vertex.Longitude / 2 - next.Longitude / 2;

				AddArc(new(controlLat, controlLon), next);
			}

			arcTo(from, to, origin);
		}

		public void Jump(Coordinate point) => _segments.Add(new InvisibleSegment(point));

		public static Route operator +(Route first, Route second) => new(first.Altitude, first._segments.Concat(second._segments).ToArray());

		public override int GetHashCode() => _segments.Aggregate(0, (s, i) => HashCode.Combine(s, i));

		public IEnumerator<RouteSegment> GetEnumerator() => ((IEnumerable<RouteSegment>)_segments).GetEnumerator();
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => ((System.Collections.IEnumerable)_segments).GetEnumerator();

		public IEnumerable<((double Latitude, double Longitude) From, (double Latitude, double Longitude) To)> ToSegments()
		{
			if (_segments.Count == 0)
				yield break;

			(double Latitude, double Longitude) prev = ((double)_segments[0].Point.Latitude, (double)_segments[0].Point.Longitude);
			(double Latitude, double Longitude) cur;

			foreach (var seg in _segments.Skip(1))
				switch (seg)
				{
					case InvisibleSegment invis:
						prev = ((double)invis.Point.Latitude, (double)invis.Point.Longitude);
						continue;

					case StraightLineSegment sls:
						cur = ((double)sls.Point.Latitude, (double)sls.Point.Longitude);
						yield return (prev, cur);
						prev = cur;
						continue;

					case ArcSegment arc:
						cur = ((double)arc.Point.Latitude, (double)arc.Point.Longitude);
						yield return (prev, cur);
						prev = cur;
						continue;
				}
		}

		public RouteSegment this[int idx]
		{
			get => _segments[idx];
			set => _segments[idx] = value;
		}

		public abstract record RouteSegment(Coordinate Point, string? PointLabel) { }

		public record StraightLineSegment(Coordinate Point, string? PointLabel) : RouteSegment(Point, PointLabel) { }
		public record ArcSegment(Coordinate ControlPoint, Coordinate End, string? PointLabel) : RouteSegment(End, PointLabel) { }
		public record InvisibleSegment(Coordinate Point) : RouteSegment(Point, null) { }
	}
}

internal static class WebeyeAirspaceDrawing
{
	public static string ToArtccPath(string position, JsonArray regionMap)
	{
		(double Lat, double Lon)[] points = [..
			regionMap.Where(i => i is JsonObject).Cast<JsonObject>().Select(p => (p["lat"]!.GetValue<double>(), p["lng"]!.GetValue<double>()))
		];

		return string.Join("\r\n", points.Append(points[0]).Select(p => $"T;{position};{p.Lat:00.0####};{p.Lon:000.0####};"));
	}

	public static string ToPolyfillPath(string position, string facility, JsonArray regionMap)
	{
		(double Lat, double Lon)[] points = [..
			regionMap.Where(i => i is JsonObject).Cast<JsonObject>().Select(p => (p["lat"]!.GetValue<double>(), p["lng"]!.GetValue<double>()))
		];

		return ToPolyfillPath(position, facility, points);
	}

	public static string ToPolyfillPath(string position, string facility, WSleeman.Osm.Way boundary) =>
		ToPolyfillPath(position, facility, [.. boundary.Nodes.Select(n => (n.Latitude, n.Longitude))]);

	public static string ToPolyfillPath(string position, string facility, (double Lat, double Lon)[] points)
	{
		string color = facility switch {
			"CTR" or "FSS" => "#151A1D",    // #7B9AAF
			"APP" or "DEP" => "#131C27",    // #70A5EC
			"TWR" => "#D54944",             // #FF5751
			_ => ""
		};

		return string.Join("\r\n", points.Append(points[0]).Select(p => $"{p.Lat:00.0####};{p.Lon:000.0####};").Prepend($"{position};{color};1;{color};"));
	}
}