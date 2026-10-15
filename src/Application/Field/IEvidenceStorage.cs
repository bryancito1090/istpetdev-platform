namespace IstpetDev.Application.Field;

public sealed record EvidenceUploadAuthorization(string Url, IReadOnlyDictionary<string, string> Headers, DateTime ExpiresAtUtc);

public interface IEvidenceStorage
{
    Task<EvidenceUploadAuthorization> AuthorizeUploadAsync(string key, string mimeType, string sha256, CancellationToken ct);
    Task VerifyAsync(string key, string mimeType, long byteSize, string sha256, CancellationToken ct);
}
