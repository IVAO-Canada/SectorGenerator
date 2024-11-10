using NetTopologySuite.IO;

using System.Collections.Frozen;
using System.Drawing;

using WSleeman.Osm;

namespace CIFPReader;
internal static class Coastline
{
	public static Dictionary<char, Way[]> LoadTopologies(string topoPath)
	{
		Dictionary<char, Way[]> topos = [];

		foreach (string level in Directory.EnumerateFiles(topoPath, "*.shp"))
		{
			ShapefileReader reader = new(level);

			var geos = reader.ReadAll().Geometries;
			topos.Add(Path.GetFileNameWithoutExtension(level)[^1], [.. geos.AsParallel().Select(g => new Way(0, [.. g.Coordinates.Select(p => new Node(0, p.Y, p.X, FrozenDictionary<string, string>.Empty))], new Dictionary<string, string>() { { "natural", "coastline" } }.ToFrozenDictionary()))]);
		}

		return topos;
	}
}
