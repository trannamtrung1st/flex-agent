using System.Text.Json;
using FlexAgent.CanonicalJson;

namespace FlexAgent.Evaluation.Domain;

public static class FrozenInputDigest
{
    private static readonly CanonicalJsonLimits Limits = new(
        maxUtf8Bytes: 64 * 1024,
        maxNestingDepth: 8,
        maxObjectProperties: 64,
        maxArrayElements: 64);

    public static string Compute(FrozenInputIdentity input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var document = new
        {
            schema = "evaluation-frozen-input-jcs-sha256-v1",
            handoff_id = input.HandoffId,
            ownership = new
            {
                organization_id = Id(input.Ownership.OrganizationId),
                activity_id = Id(input.Ownership.ActivityId),
                participant_id = Id(input.Ownership.ParticipantId),
                attempt_id = Id(input.Ownership.AttemptId),
                session_id = Id(input.Ownership.SessionId),
            },
            terminal = new
            {
                record_id = Id(input.TerminalRecordId),
                state = input.TerminalState,
                cutoff_sequence = input.CutoffSequence,
                seal_procedure_id = input.ManifestSealProcedureId,
                seal_digest = input.TerminalSealDigest,
            },
            configuration = new
            {
                id = Id(input.ConfigurationId),
                digest = input.ConfigurationDigest,
            },
            manifest = new
            {
                id = Id(input.ManifestId),
                digest = input.ManifestDigest,
            },
            rubric = Source(input.Rubric),
            submission = Source(input.Submission),
            evaluator_registry_version = input.EvaluatorRegistryVersion,
            model = new
            {
                profile_id = input.Model.ProfileId,
                profile_version = input.Model.ProfileVersion,
                profile_digest = input.Model.ProfileDigest,
                provider_id = input.Model.ProviderId,
                credential_mode = input.Model.CredentialMode,
                credential_binding_reference = input.Model.CredentialBindingReference,
                credential_binding_version = input.Model.CredentialBindingVersion,
            },
            lifecycle_policy_ref = input.LifecyclePolicyRef,
        };

        return CanonicalJsonProcessor.CanonicalizeSha256Hex(
            JsonSerializer.SerializeToUtf8Bytes(document),
            Limits);
    }

    private static object Source(ExactSourceIdentity source) => new
    {
        source_key = source.SourceKey,
        source_id = Id(source.SourceId),
        source_version_id = Id(source.SourceVersionId),
        content_digest = source.ContentDigest,
    };

    private static string Id(Guid value) => value.ToString("D");
}
