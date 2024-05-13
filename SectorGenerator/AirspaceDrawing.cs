using CIFPReader;

using System.Text.Json.Nodes;

using static CIFPReader.ControlledAirspace;

namespace SectorGenerator;

internal class CifpAirspaceDrawing(IEnumerable<Airspace> cifpAirspaces)
{
	private readonly Airspace[] _airspaces = [.. cifpAirspaces];
	private readonly (AirspaceClass AsClass, (float Latitude, float Longitude) From, (float Latitude, float Longitude) To)[] _linearized = [.. cifpAirspaces.SelectMany(LinearizeAirspace)];

	private static (AirspaceClass AsClass, (float Latitude, float Longitude) From, (float Latitude, float Longitude) To)[] LinearizeAirspace(Airspace asp)
	{
		List<(AirspaceClass AsClass, (float Latitude, float Longitude) From, (float Latitude, float Longitude) To)> segments = [];

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

	public string ClassBPaths => string.Join("\r\nT;Dummy;N000.00.00.000;W000.00.00.000;\r\n",
		_linearized.Where(l => l.AsClass == AirspaceClass.B).Select(l =>
			$"T;CLASS B;{l.From.Latitude:00.0####};{l.From.Longitude:000.0####};\r\nT;CLASS B;{l.To.Latitude:00.0####};{l.To.Longitude:000.0####};"
		)
	);

	public string ClassCPaths => string.Join("\r\nT;Dummy;N000.00.00.000;W000.00.00.000;\r\n",
		_linearized.Where(l => l.AsClass == AirspaceClass.C).Select(l =>
			$"T;CLASS B;{l.From.Latitude:00.0####};{l.From.Longitude:000.0####};\r\nT;CLASS B;{l.To.Latitude:00.0####};{l.To.Longitude:000.0####};"
		)
	);

	public string ClassDPaths => string.Join("\r\nT;Dummy;N000.00.00.000;W000.00.00.000;\r\n",
		_linearized.Where(l => l.AsClass == AirspaceClass.D).Select(l =>
			$"T;CLASS B;{l.From.Latitude:00.0####};{l.From.Longitude:000.0####};\r\nT;CLASS B;{l.To.Latitude:00.0####};{l.To.Longitude:000.0####};"
		)
	);

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
			float getAngle(float degrees, float degrees2)
			{
				if (Math.Abs(degrees - degrees2) < 0.001)
					return 0;

				if (degrees > degrees2)
				{
					float num = degrees2 + 360 - degrees;
					float num2 = degrees - degrees2;
					if (num2 < num)
					{
						return -num2;
					}

					return num;
				}

				float num3 = degrees2 - degrees;
				float num4 = degrees + 360 - degrees2;

				return (num4 < num3) ? -num4 : num3;
			}

			float clampAngle(float angle)
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
				float startBearing = (float?)startData.bearing?.ToTrue()?.Degrees ?? (vertex.Latitude > origin.Latitude ? 0 : 180);
				float endBearing = (float?)endData.bearing?.ToTrue()?.Degrees ?? (next.Latitude > origin.Latitude ? 0 : 180);

				float guessBearing = getAngle(startBearing, endBearing);
				float realBearing = clampAngle(guessBearing / 2 + startBearing);

				if ((clockwise && guessBearing < 0) || (!clockwise && guessBearing > 0))
					realBearing = clampAngle(realBearing + 180);

				var midPoint = origin.FixRadialDistance(new TrueCourse((decimal)realBearing), startData.distance);

				if (midPoint != vertex && midPoint != next && Math.Abs(clampAngle(getAngle(startBearing, realBearing) + getAngle(realBearing, endBearing))) > 5)
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

		public IEnumerable<((float Latitude, float Longitude) From, (float Latitude, float Longitude) To)> ToSegments()
		{
			if (_segments.Count == 0)
				yield break;

			(float Latitude, float Longitude) prev = ((float)_segments[0].Point.Latitude, (float)_segments[0].Point.Longitude);
			(float Latitude, float Longitude) cur;

			foreach (var seg in _segments.Skip(1))
				switch (seg)
				{
					case InvisibleSegment invis:
						prev = ((float)invis.Point.Latitude, (float)invis.Point.Longitude);
						continue;

					case StraightLineSegment sls:
						cur = ((float)sls.Point.Latitude, (float)sls.Point.Longitude);
						yield return (prev, cur);
						prev = cur;
						continue;

					case ArcSegment arc:
						cur = ((float)arc.Point.Latitude, (float)arc.Point.Longitude);
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

		return string.Join("\r\n", points.Append(points[0]).Select(p =>$"T;{position};{p.Lat:00.0####};{p.Lon:000.0####};"));
	}

	public static string ToPolyfillPath(string position, string facility, JsonArray regionMap)
	{
		(double Lat, double Lon)[] points = [..
			regionMap.Where(i => i is JsonObject).Cast<JsonObject>().Select(p => (p["lat"]!.GetValue<double>(), p["lng"]!.GetValue<double>()))
		];

		return ToPolyfillPath(position, facility, points);
	}

	public static string ToPolyfillPath(string position, string facility, WSleeman.Osm.Way boundary) =>
		ToPolyfillPath(position, facility, [..boundary.Nodes.Select(n => (n.Latitude, n.Longitude))]);

	public static string ToPolyfillPath(string position, string facility, (double Lat, double Lon)[] points)
	{
		string color = facility switch {
			"CTR" or "FSS" => "#151A1D",	// #7B9AAF
			"APP" or "DEP" => "#131C27",	// #70A5EC
			"TWR" => "#D54944",             // #FF5751
			_ => ""
		};

		return string.Join("\r\n", points.Append(points[0]).Select(p => $"{p.Lat:00.0####};{p.Lon:000.0####};").Prepend($"{position};{color};1;{color};"));
	}
}