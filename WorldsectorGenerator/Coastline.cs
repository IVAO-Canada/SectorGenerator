using NetTopologySuite.IO;

namespace WorldsectorGenerator;

internal static class Coastline
{
	public static Dictionary<char, Boundary[]> LoadTopologies(string topoPath)
	{
		Dictionary<char, Boundary[]> topos = [];

		foreach (string level in Directory.EnumerateFiles(topoPath, "*.shp"))
		{
			ShapefileReader reader = new(level);

			var geos = reader.ReadAll().Geometries;

			topos.Add(Path.GetFileNameWithoutExtension(level)[^1], [.. geos.AsParallel().Select(g => new Boundary([.. g.Coordinates.Select(p => new Point(p.Y, p.X))]))]);
		}

		return topos;
	}
}
