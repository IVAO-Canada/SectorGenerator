using Amazon.S3;

using CIFPReader;

Console.WriteLine("Loading CIFPs.");
DateTimeOffset start = DateTimeOffset.UtcNow;
_ = CIFP.Load("");
DateTimeOffset processed = DateTimeOffset.UtcNow;
Console.WriteLine($"Loading completed in {(processed - start).TotalSeconds:0.000} seconds.");
AmazonS3Client s3 = new();
foreach (string filename in Directory.EnumerateFiles("cifp"))
{
	CancellationTokenSource cts = new(TimeSpan.FromSeconds(30));
	Console.WriteLine($"Uploading {filename} to s3://ivao-xa/{Path.GetFileName(filename)}...");
	_ = await s3.PutObjectAsync(new() {
		BucketName = "ivao-xa",
		Key = Path.GetFileName(filename),
		ContentBody = File.ReadAllText(filename),
		ContentType = "application/json",
	}, cts.Token);
	Console.WriteLine("Done!");
}
Console.WriteLine($"Uploading completed in {(DateTimeOffset.UtcNow - processed).TotalSeconds:0.000} seconds.");