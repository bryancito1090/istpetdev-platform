using IstpetDev.Domain.Common;
using NetTopologySuite.Geometries;

namespace IstpetDev.Domain.Entities;

public sealed class Conversation : OrganizationEntity
{
    public Guid? DeliveryId { get; set; }
    public Guid? RoutePlanId { get; set; }
    public Guid CreatedByAccountId { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class ConversationParticipant : OrganizationEntity
{
    public Guid ConversationId { get; set; }
    public Guid OrganizationAccessId { get; set; }
    public DateTime JoinedAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
}

public sealed class Message : OrganizationEntity
{
    public Guid ConversationId { get; set; }
    public Guid AuthorAccountId { get; set; }
    public Guid OperationId { get; set; }
    public string BodyText { get; set; } = string.Empty;
    public DateTime ReceivedAtServer { get; set; }
    public DateTime? RedactedAtUtc { get; set; }
}

public sealed class MessageAttachment : OrganizationEntity
{
    public Guid MessageId { get; set; }
    public Guid StoredFileId { get; set; }
}

public sealed class Notification : OrganizationEntity
{
    public Guid RecipientAccessId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public DateTime? ReadAtUtc { get; set; }
    public Guid? SourceEventId { get; set; }
}

public sealed class IdempotencyRecord : OrganizationEntity
{
    public Guid AccountId { get; set; }
    public Guid OperationId { get; set; }
    public string ActionCode { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;
    public int? ResponseStatus { get; set; }
    public string? ResponseJson { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime RetainUntilUtc { get; set; }
}

public sealed class OutboxEvent : OrganizationEntity
{
    public string EventType { get; set; } = string.Empty;
    public int SchemaVersion { get; set; }
    public Guid AggregateId { get; set; }
    public string AggregateType { get; set; } = string.Empty;
    public long AggregateVersion { get; set; }
    public string PayloadJson { get; set; } = "{}";
    public DateTime OccurredAtServer { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
    public int Attempts { get; set; }
}

public sealed class AuditEvent : OrganizationEntity
{
    public Guid? ActorAccountId { get; set; }
    public string ActionCode { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public Guid? ResourceId { get; set; }
    public string ResultCode { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
    public DateTime RecordedAtUtc { get; set; }
}

public sealed class SyncBatch : OrganizationEntity
{
    public Guid AccountId { get; set; }
    public string DeviceReference { get; set; } = string.Empty;
    public Guid OperationId { get; set; }
    public DateTime ReceivedAtServer { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class SyncOperation : OrganizationEntity
{
    public Guid SyncBatchId { get; set; }
    public Guid IdempotencyRecordId { get; set; }
    public string ActionCode { get; set; } = string.Empty;
    public DateTime ClientOccurredAt { get; set; }
    public string ResultCode { get; set; } = string.Empty;
}
