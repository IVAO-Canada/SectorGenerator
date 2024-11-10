using System.Text.Json.Serialization;

namespace WorldsectorGenerator;

internal record Point([property: JsonPropertyName("lat")] double Latitude, [property: JsonPropertyName("lng")]  double Longitude) { }

internal record Boundary(Point[] Points) { }