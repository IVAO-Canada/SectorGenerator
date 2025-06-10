using System.Text.RegularExpressions;

namespace ManualAdjustments;

public static partial class Parsing
{
	public static ParseResult Parse(string input)
	{
		Parser parser = new(input);

		return parser.ParseFile(0, []);
	}

	internal static PossiblyResolvedWaypoint ParseWaypoint(string? name, string? coordinate, string? radialDist)
	{
		CIFPReader.ICoordinate? coord = null;
		CIFPReader.UnresolvedWaypoint? waypoint = null;
		CIFPReader.UnresolvedFixRadialDistance? radial = null;

		if (coordinate is string cStr)
		{
			Match match = CoordinateRegex().Match(cStr);
			if (!match.Success)
				throw new ArgumentException("Invalid coordinate.", nameof(coordinate));

			decimal lat = decimal.Parse(match.Groups["lat"].Value);
			decimal lon = decimal.Parse(match.Groups["lon"].Value);

			coord = new CIFPReader.Coordinate(lat, lon);
		}

		if (name is string nStr)
		{
			nStr = nStr.Trim('"');
			waypoint = new(nStr);

			if (coord is CIFPReader.Coordinate c)
				coord = new CIFPReader.NamedCoordinate(nStr, c);
		}

		if (radialDist is string rStr)
		{
			if (coord is null && waypoint is null)
				throw new ArgumentNullException(nameof(coordinate));

			Match m = RadialDistRegex().Match(rStr);
			if (!m.Success)
				throw new ArgumentException("Invalid radial/distance.", nameof(radialDist));

			decimal magCourse = decimal.Parse(m.Groups["radial"].Value);
			decimal distance = decimal.Parse(m.Groups["dist"].Value);

			if (coord is null)
				radial = new(waypoint!, new(magCourse, null), distance);
			else
			{
				waypoint = null;
				// TODO: Figure out how to get this into a magnetic course. Pass in a CIFP?
				coord = coord.GetCoordinate().FixRadialDistance(new CIFPReader.TrueCourse(magCourse), distance);
			}
		}

		return new(coord, waypoint, radial);
	}
}
