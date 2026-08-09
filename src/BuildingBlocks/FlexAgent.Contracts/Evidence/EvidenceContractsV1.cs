namespace FlexAgent.Contracts.Evidence;

public sealed record EvidenceLocatorV1(
    string LocatorSchema,
    string SourceType,
    EvidenceSourceRefV1 SourceRef,
    EvidenceOwnershipRefV1 OwnershipRef,
    object Location,
    string Precision,
    EvidenceIntegrityV1 Integrity,
    EvidenceCreatedByV1 CreatedBy);

public sealed record EvidenceSourceRefV1(
    string SourceId,
    string SourceVersion,
    long? TerminalCutoffSequence);

public sealed record EvidenceOwnershipRefV1(
    string OrganizationId,
    string ActivityId,
    string ParticipantId,
    string AttemptId,
    string SessionId,
    string EvaluationId);

public sealed record EvidenceIntegrityV1(
    string SourceDigest,
    string AdapterVersion,
    string VerificationState);

public sealed record EvidenceCreatedByV1(string ServiceId, string InvocationId);

public sealed record WholeItemLocationV1(string LocationType, string ItemId);

public sealed record Utf8ByteRangeLocationV1(
    string LocationType,
    string ItemId,
    int StartInclusive,
    int EndExclusive,
    string? ExcerptDigest);

public sealed record JsonPointerLocationV1(string LocationType, string JsonPointer);
