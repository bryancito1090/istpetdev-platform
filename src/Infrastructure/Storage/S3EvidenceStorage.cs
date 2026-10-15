using System.Net;
using System.Security.Cryptography;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using IstpetDev.Application.Field;
using Microsoft.Extensions.Configuration;

namespace IstpetDev.Infrastructure.Storage;

/// <summary>Private S3 objects. No credentials or public read URLs are returned to the PWA.</summary>
public sealed class S3EvidenceStorage(IConfiguration configuration, Func<AmazonS3Client>? clientFactory = null) : IEvidenceStorage
{
    private string Bucket => configuration["Evidence:S3:Bucket"] is { Length: > 0 } bucket
        ? bucket : throw new FieldException(503, "EVIDENCE_STORAGE_UNAVAILABLE", "Configura Evidence:S3:Bucket para subir evidencias.");

    private AmazonS3Client Client() => clientFactory?.Invoke() ?? new(RegionEndpoint.GetBySystemName(configuration["Evidence:S3:Region"] ?? "us-east-1"));

    public async Task<EvidenceUploadAuthorization> AuthorizeUploadAsync(string key, string mimeType, string sha256, CancellationToken ct)
    {
        var bucket = Bucket;
        using var client = Client();
        var expires = DateTime.UtcNow.AddMinutes(10);
        var checksum = Convert.ToBase64String(Convert.FromHexString(sha256));
        var request = new GetPreSignedUrlRequest { BucketName = bucket, Key = key, Verb = HttpVerb.PUT, Expires = expires, ContentType = mimeType };
        request.Headers["x-amz-checksum-sha256"] = checksum;
        var url = await client.GetPreSignedURLAsync(request);
        return new(url, new Dictionary<string, string> { ["Content-Type"] = mimeType, ["x-amz-checksum-sha256"] = checksum }, expires);
    }

    public async Task VerifyAsync(string key, string mimeType, long byteSize, string sha256, CancellationToken ct)
    {
        var bucket = Bucket;
        using var client = Client();
        try
        {
            using var response = await client.GetObjectAsync(bucket, key, ct);
            if (response.ContentLength != byteSize || response.Headers.ContentType != mimeType)
                throw new FieldUnprocessableException("EVIDENCE_MISMATCH", "El tamaño o tipo del archivo no coincide con la captura.");
            var digest = await SHA256.HashDataAsync(response.ResponseStream, ct);
            if (!Convert.ToHexString(digest).Equals(sha256, StringComparison.OrdinalIgnoreCase))
                throw new FieldUnprocessableException("EVIDENCE_MISMATCH", "El hash de la evidencia no coincide.");
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new FieldException(503, "EVIDENCE_NOT_UPLOADED", "La foto todavía no está almacenada. Reintenta su carga.");
        }
    }
}
