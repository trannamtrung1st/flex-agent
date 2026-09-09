using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Infrastructure;

public static class BuiltinEvaluatorImplementationBinding
{
    public const string RunnerSourceArtifactDigest =
        "49dc731a4c5ea04e326565b09459421f42c66b103608feb0d5e0c3fee3cf6fb3";

    public const string DeterministicExecutionBoundsSourceArtifactDigest =
        "52cd0eeed553003fa9796ae50877a24451724609774659199a494f4f2ee70e38";

    public const string InProcessExecutionContractSourceArtifactDigest =
        "5a6cbde701b305541c205bbb3c716249cf971787babb2d838002aa78c2d5eee2";

    public const string EvaluationIdentitySourceArtifactDigest =
        "bc5c2b30137e3411632a51cbe0e0838073631f74885abd0b49173421c554496e";

    public const string EvaluationPositiveDurationSourceArtifactDigest =
        "bf3dcae11c62b559d52272f92d177c4dccd206964dc58a1c089461a852c3e16c";

    public const string EvaluationProjectSourceArtifactDigest =
        "c2ed165823783e0445b3002afb12b58bf2c1307bd1c10fd30f731e4ea3895dbc";

    public const string EvaluationInfrastructureProjectSourceArtifactDigest =
        "d5b07dd4d033902d5e1ca47369396462fc11c220ef5dcf9727f60376c9e31aad";

    private static readonly IReadOnlyDictionary<string, string> SourceArtifactDigests =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["deterministic_execution_bounds"] = DeterministicExecutionBoundsSourceArtifactDigest,
            ["evaluation_identity"] = EvaluationIdentitySourceArtifactDigest,
            ["evaluation_positive_duration"] = EvaluationPositiveDurationSourceArtifactDigest,
            ["flex_agent.evaluation.csproj"] = EvaluationProjectSourceArtifactDigest,
            ["flex_agent.evaluation.infrastructure.csproj"] = EvaluationInfrastructureProjectSourceArtifactDigest,
            ["in_process_execution_contract"] = InProcessExecutionContractSourceArtifactDigest,
            ["runner"] = RunnerSourceArtifactDigest,
        };

    public static readonly BuiltinEvaluatorImplementationIdentity Identity =
        BuiltinEvaluatorImplementationManifest.CreateIdentity(SourceArtifactDigests);
}
