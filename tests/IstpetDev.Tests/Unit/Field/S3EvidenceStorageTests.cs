using System.Net;
using System.Security.Cryptography;
using System.Text;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using IstpetDev.Application.Field;
using IstpetDev.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;

namespace IstpetDev.Tests.Unit.Field;

[Trait("Category", "Unit")]
public sealed class S3EvidenceStorageTests
{
    private static IConfiguration Config(bool configured = true) => new ConfigurationBuilder().AddInMemoryCollection(
        configured ? new Dictionary<string, string?> { ["Evidence:S3:Bucket"] = "test-private-evidence" } : new()).Build();
    private static readonly byte[] Photo = Encoding.UTF8.GetBytes("test-image-bytes");
    private static string Hash => Convert.ToHexString(SHA256.HashData(Photo)).ToLowerInvariant();
    private static BasicAWSCredentials Credentials => new("TESTKEY", new string('a', 40));

    private sealed class StoredObjectClient(bool missing = false) : AmazonS3Client(Credentials, RegionEndpoint.USEast1)
    {
        public override Task<GetObjectResponse> GetObjectAsync(string bucketName, string key, CancellationToken ct = default)
        {
            if (missing) throw new AmazonS3Exception("Not found") { StatusCode = HttpStatusCode.NotFound };
            var response = new GetObjectResponse { ContentLength = Photo.Length, ResponseStream = new MemoryStream(Photo) };
            response.Headers.ContentType = "image/jpeg";
            return Task.FromResult(response);
        }
    }

    [Fact]
    public async Task Presigns_private_put_with_checksum_and_short_expiry_without_contacting_AWS()
    {
        var storage = new S3EvidenceStorage(Config(), () => new AmazonS3Client(Credentials, RegionEndpoint.USEast1));
        var result = await storage.AuthorizeUploadAsync("evidences/test/photo", "image/jpeg", Hash, default);
        Assert.Contains("X-Amz-Algorithm=AWS4-HMAC-SHA256", result.Url);
        Assert.Contains("X-Amz-Expires=", result.Url);
        Assert.Equal(Convert.ToBase64String(SHA256.HashData(Photo)), result.Headers["x-amz-checksum-sha256"]);
        Assert.Equal("image/jpeg", result.Headers["Content-Type"]);
        Assert.InRange(result.ExpiresAtUtc, DateTime.UtcNow.AddMinutes(9), DateTime.UtcNow.AddMinutes(11));
    }

    [Fact]
    public async Task Verifies_real_object_bytes_type_size_and_hash()
    {
        var storage = new S3EvidenceStorage(Config(), () => new StoredObjectClient());
        await storage.VerifyAsync("key", "image/jpeg", Photo.Length, Hash, default);
        await Assert.ThrowsAsync<FieldUnprocessableException>(() => storage.VerifyAsync("key", "image/png", Photo.Length, Hash, default));
        await Assert.ThrowsAsync<FieldUnprocessableException>(() => storage.VerifyAsync("key", "image/jpeg", Photo.Length + 1, Hash, default));
        await Assert.ThrowsAsync<FieldUnprocessableException>(() => storage.VerifyAsync("key", "image/jpeg", Photo.Length, new string('0', 64), default));
    }

    [Fact]
    public async Task Missing_object_or_configuration_keeps_evidence_pending()
    {
        var missing = new S3EvidenceStorage(Config(), () => new StoredObjectClient(missing: true));
        var error = await Assert.ThrowsAsync<FieldException>(() => missing.VerifyAsync("key", "image/jpeg", Photo.Length, Hash, default));
        Assert.Equal(503, error.StatusCode);
        var unconfigured = new S3EvidenceStorage(Config(configured: false), () => throw new InvalidOperationException("No AWS client should be created"));
        Assert.Equal(503, (await Assert.ThrowsAsync<FieldException>(() => unconfigured.AuthorizeUploadAsync("key", "image/jpeg", Hash, default))).StatusCode);
    }
}
