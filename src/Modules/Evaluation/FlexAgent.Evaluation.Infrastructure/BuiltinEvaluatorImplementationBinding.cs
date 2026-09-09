using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Infrastructure;

public static class BuiltinEvaluatorImplementationBinding
{
    public const string RunnerSourceArtifactDigest =
        "49dc731a4c5ea04e326565b09459421f42c66b103608feb0d5e0c3fee3cf6fb3";

    public static readonly BuiltinEvaluatorImplementationIdentity Identity =
        BuiltinEvaluatorImplementationManifest.CreateIdentity(RunnerSourceArtifactDigest);
}
