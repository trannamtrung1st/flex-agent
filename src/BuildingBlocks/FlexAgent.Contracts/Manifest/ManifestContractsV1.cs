namespace FlexAgent.Contracts.Manifest;

public sealed record ResolvedExecutionManifestV1(
    string SchemaVersion,
    string ManifestId,
    SessionOwnershipRefV1 Ownership,
    ConfigurationRefV1 ConfigurationRef,
    IReadOnlyList<ManifestRuntimeRecordV1> RuntimeRecords,
    string TerminalState,
    TerminalSealV1? TerminalSeal);

public sealed record SessionOwnershipRefV1(
    string OrganizationId,
    string ActivityId,
    string ParticipantId,
    string AttemptId,
    string SessionId);

public sealed record ConfigurationRefV1(string ConfigurationId, string ConfigurationDigest);

public sealed record ManifestRuntimeRecordV1(
    string Sequence,
    string RecordType,
    string ServiceActor,
    string OccurredAt,
    ProtectedPayloadRefV1 PayloadRef);

public sealed record ProtectedPayloadRefV1(string ProtectedRef, string ContentDigest);

public sealed record TerminalSealV1(string ProcedureId, string SealDigest);
