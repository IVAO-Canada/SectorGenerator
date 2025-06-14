using CIFPReader;

using System.Text.RegularExpressions;

if (args.Length is 0)
{
	Console.Write("Centrepoint (fix name): ");
	string centrePointInput = Console.ReadLine()!;
	Console.Write("Coordinate (DDMMSSVDDDMMSSH; V = N/S, H = E/W): ");
	args = [centrePointInput, Console.ReadLine()!];
}

Regex inputRegex = new(
	@"^(?<latDeg>\d\d)(?<latMin>\d\d)(?<latSec>\d\d)(?<latHemisphere>[NS])(?<lonDeg>\d\d\d)(?<lonMin>\d\d)(?<lonSec>\d\d)(?<lonHemisphere>[EW])$",
	RegexOptions.ExplicitCapture
);

string centrepointStr = args[0].Trim();
string coordStr = args[1].Trim();
Match match = inputRegex.Match(coordStr);

if (!match.Success)
{
	Console.WriteLine("Invalid coordinate: Remember: DDMMSSVDDDMMSSH where V is N or S and H is E or W. e.g. 373306N1164254W");
	return -1;
}

decimal latDeg = decimal.Parse(match.Groups["latDeg"].Value),
		latMin = decimal.Parse(match.Groups["latMin"].Value),
		latSec = decimal.Parse(match.Groups["latSec"].Value),
		lonDeg = decimal.Parse(match.Groups["lonDeg"].Value),
		lonMin = decimal.Parse(match.Groups["lonMin"].Value),
		lonSec = decimal.Parse(match.Groups["lonSec"].Value);

decimal latitude = latDeg + (latMin * 0.0166666m) + (latSec * 0.002777777m),
		longitude = lonDeg + (lonMin * 0.0166666m) + (lonSec * 0.002777777m);

if (match.Groups["latHemisphere"].Value is "S")
	latitude = -latitude;

if (match.Groups["lonHemisphere"].Value is "W")
	longitude = -longitude;

Coordinate coord = new(latitude, longitude);

Console.WriteLine("Getting data from internet.");
CIFP cifp = CIFP.Load("http://ivao-us.s3-website-us-west-2.amazonaws.com/reduced/");

Coordinate centrePoint;

if (cifp.Navaids.TryGetValue(centrepointStr, out var navaidOpts) && navaidOpts.Count is not 0)
	centrePoint = navaidOpts.MinBy(n => n.Position.DistanceTo(coord))!.Position;
else if (cifp.Fixes.TryGetValue(centrepointStr, out var fixOpts) && fixOpts.Count is not 0)
	centrePoint = fixOpts.Select(static f => f.GetCoordinate()).MinBy(f => f.DistanceTo(coord))!;
else if (cifp.Aerodromes.TryGetValue(centrepointStr, out var ap))
	centrePoint = ap.Location.GetCoordinate();
else
	throw new Exception("Could not find centrepoint");

var res = centrePoint.GetBearingDistance(coord);
Console.WriteLine($"{centrepointStr} {res.bearing!.Degrees:000}° @ {res.distance:00.00}nmi");

return 0;
