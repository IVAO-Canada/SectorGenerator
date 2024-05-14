using System.Text.Json.Nodes;
using WorldsectorGenerator;

Console.Write("Getting IVAO API token..."); await Console.Out.FlushAsync();
string apiToken, apiRefreshToken;
using (Oauth oauth = new())
{
	JsonNode jsonNode = await oauth.GetOpenIdFromRefreshTokenAsync("eyJhbGciOiJSUzUxMiIsInR5cCI6IkpXVCIsImtpZCI6ImYxNjMyZTUxNjYzMzYyMmIxZmY5MGM5ZjE5YTgxMDJhODA5YTVjOWVjZWEyIn0.eyJhdWQiOiJkNTY5ZTVhNi0zNjdjLTQwMTQtOTg5Mi04MjM5ZjMzOWJhY2MiLCJzdWIiOiI2NDQ4OTkiLCJzY29wZSI6Im9wZW5pZCIsInBlcm1pc3Npb24iOiJkaXNwbGF5X2FsbF9zZXNzaW9ucyBkaXNwbGF5X2FsbF9zZXNzaW9uczpVUyIsInR5cGUiOiJyZWZyZXNoX3Rva2VuIiwiaXNzIjoiaHR0cHM6Ly9hcGkuaXZhby5hZXJvIiwiaWF0IjoxNzE1NjYyMTQxLCJleHAiOjE3MzE3MzI1NDEsImp0aSI6IjBhRXB6SEt6eXpyRmMyaXMxZVhZSHhObGpJYjBjNGl4NUNpRG5XOVlxQ2M9In0.SqsUJeX21PcTi_NhjFlxlq0zdoPrhVK7Shr3JbIwp0N-cjjVTgx9p2-_Aarh2D2Fjqh9cWGVHWRzEwjM9OK22kvwHrd4HRBWf_lLOm_BlUgDaQIgJ5XyY_cxWh2wCya6Yw37JaFEN9foUK6i4taJc41vvTgxj-M64fbJSg-7VkzTsLSoQAJl1qZqRj4t4YtmYlZZdR1cGPIrjrzC0L6uicc4c8u7VWylHNSAIHYNXUkfhhBWSp07f-Qq4s5hZ1Bd_SVf8gQF3MY3G1-wHjkoljcUlZAqa6XYA5lAIr5f0OqONGmoFqjVKuPu62UzRiOIIjWKPG-oFIQH18lbOXumnyVe6VgmBwBOYgqT_n8Kr8CXSL0Tep57cZ8DDWJd0OmXf2_y4TQE5jMNeP7MVKJXiqRaRtDvv2Zzw36mqFNg9LPxUhf45KCoeFPFGYfnprlj0dKZVMQyWc7cBNgBHcLg84KqhEWsdekMWzRaCSr3vw_4-28rYkwsQyu9y3LJ4nG9");

	apiToken = jsonNode["access_token"]!.GetValue<string>();
	apiRefreshToken = jsonNode["refresh_token"]!.GetValue<string>();
}
Console.WriteLine($" Done! (Refresh: {apiRefreshToken})");

Boundary[] coastlines = Coastline.LoadTopologies("coastline")['i'];
WebeyeGrabber webeye = new(apiToken);
(string Position, string Facility, Boundary Boundary)[] bigShapes = [.. (await webeye.GetSubcentersAsync()).Where(kvp => kvp.Boundary.Points.Length > 1)];
(string Position, string Facility, Boundary Boundary)[] littleShapes = [..(await webeye.GetAtcPositionsAsync()).Where(kvp => kvp.Boundary.Points.Length > 1)];

File.WriteAllText("Worldsector Fast.isc", $@"// Developed by Wes (644899)
[INFO]
N000.00.00.000
E000.00.00.000
60
60
0

[DEFINE]
CTR;#151A1D;
FSS;#151A1D;
APP;#131C27;
DEP;#131C27;
TWR;#D54944;

[ARTCC]
{string.Join("\r\n",
	bigShapes
	.Where(kvp => kvp.Facility == "CTR" && kvp.Position.Count(c => c == '_') == 1)
	.Select(kvp => string.Join("\r\n",
		$"L;{kvp.Position};{kvp.Boundary.Points.Average(p => p.Latitude):00.0####};{kvp.Boundary.Points.Average(p => p.Longitude):000.0####};"
	))
)}

[GEO]
{string.Join("\r\n",
	coastlines
	.Where(c => c.Points.Length > 1)
	.SelectMany(c => c.Points[..^1].Zip(c.Points[1..]))
	.Select(ps => $"{ps.First.Latitude:00.0####};{ps.First.Longitude:000.0####};{ps.Second.Latitude:00.0####};{ps.Second.Longitude:000.0####};COAST;")
)}

[FILLCOLOR]
{string.Join("\r\n",
	bigShapes.Concat(littleShapes)
	.Select(kvp => string.Join("\r\n",
		kvp.Boundary.Points.Select(p => $"{p.Latitude:00.0####};{p.Longitude:000.0####};")
		.Prepend($"{kvp.Position};{kvp.Facility};1;{kvp.Facility};")
	))
)}
");

File.WriteAllText("Worldsector Slow.isc", $@"// Developed by Wes (644899)
[INFO]
N000.00.00.000
E000.00.00.000
60
60
0

[DEFINE]
CTR;#151A1D;
FSS;#151A1D;
APP;#131C27;
DEP;#131C27;
TWR;#D54944;

[ARTCC]
{string.Join("\r\n",
	bigShapes
	.Where(kvp => kvp.Facility == "CTR" && kvp.Position.Count(c => c == '_') == 1)
	.Select(kvp => string.Join("\r\n",
		kvp.Boundary.Points.Select(p => $"T;{kvp.Position};{p.Latitude:00.0####};{p.Longitude:000.0####};")
		.Prepend($"L;{kvp.Position};{kvp.Boundary.Points.Average(p => p.Latitude):00.0####};{kvp.Boundary.Points.Average(p => p.Longitude):000.0####};")
	))
)}

[GEO]
{string.Join("\r\n",
	coastlines
	.Where(c => c.Points.Length > 1)
	.SelectMany(c => c.Points[..^1].Zip(c.Points[1..]))
	.Select(ps => $"{ps.First.Latitude:00.0####};{ps.First.Longitude:000.0####};{ps.Second.Latitude:00.0####};{ps.Second.Longitude:000.0####};COAST;")
)}

[FILLCOLOR]
{string.Join("\r\n",
	bigShapes.Concat(littleShapes)
	.Select(kvp => string.Join("\r\n",
		kvp.Boundary.Points.Select(p => $"{p.Latitude:00.0####};{p.Longitude:000.0####};")
		.Prepend($"{kvp.Position};{kvp.Facility};1;{kvp.Facility};")
	))
)}
");