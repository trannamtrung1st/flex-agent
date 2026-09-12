namespace FlexAgent.Evaluation.Application;

public sealed record VerifiedPermittedEvidenceMaterial(
    string EvidenceStableId,
    Guid EvidenceId,
    string SourceType,
    string ContentDigest);
