using Amazon.S3;
using Amazon.S3.Transfer;

using CIFPReader;

Console.WriteLine("Loading CIFPs.");
DateTimeOffset start = DateTimeOffset.UtcNow;
_ = CIFP.Load("");
DateTimeOffset processed = DateTimeOffset.UtcNow;
Console.WriteLine($"Loading completed in {(processed - start).TotalSeconds:0.000} seconds.");
AmazonS3Client s3 = new();
TransferUtility transfer = new(s3);
await transfer.UploadDirectoryAsync("cifp", "ivao-xa", "*.json", SearchOption.TopDirectoryOnly);
Console.WriteLine($"Uploading completed in {(DateTimeOffset.UtcNow - processed).TotalSeconds:0.000} seconds.");