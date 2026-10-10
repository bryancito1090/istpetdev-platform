using System.Security.Cryptography;

namespace IstpetDev.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; set; } = NewId();
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public long Version { get; set; }

    // .NET 8 has no Guid.CreateVersion7. Timestamp + random bytes, RFC 9562.
    public static Guid NewId()
    {
        Span<byte> bytes = stackalloc byte[16];
        RandomNumberGenerator.Fill(bytes);
        var milliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        for (var i = 5; i >= 0; i--) { bytes[i] = (byte)milliseconds; milliseconds >>= 8; }
        bytes[6] = (byte)((bytes[6] & 0x0f) | 0x70);
        bytes[8] = (byte)((bytes[8] & 0x3f) | 0x80);
        return new Guid(bytes, bigEndian: true);
    }
}

public abstract class OrganizationEntity : BaseEntity
{
    public Guid OrganizationId { get; set; }
}
