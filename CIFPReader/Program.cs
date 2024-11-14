using CIFPReader;

Console.WriteLine("Loading CIFPs.");
DateTimeOffset start = DateTimeOffset.UtcNow;
_ = CIFP.Load("cifps");
Console.WriteLine($"Loading completed in {(DateTimeOffset.UtcNow - start).TotalSeconds:0.000} seconds.");