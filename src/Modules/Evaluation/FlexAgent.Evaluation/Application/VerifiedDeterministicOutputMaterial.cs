using FlexAgent.Contracts.Manifest;
using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Application;

public sealed record VerifiedDeterministicOutputMaterial(
    EvaluationSafeFactProjection Projection,
    ProtectedPayloadRefV1 ProtectedRef);
