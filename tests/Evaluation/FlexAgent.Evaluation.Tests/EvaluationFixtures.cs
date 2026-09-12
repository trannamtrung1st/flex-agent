using FlexAgent.Contracts.Evaluation;
using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Domain;


namespace FlexAgent.Evaluation.Tests;

internal static class EvaluationFixtures
{
    public static string RubricDigest { get; } = new string('b', 64);

    public static string ConfigurationDigest { get; } = new string('c', 64);

    public static string ManifestDigest { get; } = new string('d', 64);

    public static string TerminalSealDigest { get; } = new string('f', 64);

    public static string SubmissionDigest { get; } = new string('e', 64);

    public static EvaluationOwnership Ownership() =>
        EvaluationOwnership.TryCreate(
            Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa"),
            Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb"),
            Guid.Parse("cccccccc-cccc-4ccc-8ccc-cccccccccccc"),
            Guid.Parse("dddddddd-dddd-4ddd-8ddd-dddddddddddd"),
            Guid.Parse("eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee")).Value!;

    public static FrozenModelIdentity Model() =>
        FrozenModelIdentity.TryCreate(
            EvaluationSyntheticDevelopmentModelProfile.ProfileId,
            EvaluationSyntheticDevelopmentModelProfile.ProfileVersion,
            new string('a', 64),
            EvaluationSyntheticDevelopmentModelProfile.ProviderId,
            EvaluationSyntheticDevelopmentModelProfile.CredentialMode,
            EvaluationSyntheticDevelopmentModelProfile.CredentialBindingReference,
            EvaluationSyntheticDevelopmentModelProfile.CredentialBindingVersion).Value!;

    public static ExactSourceIdentity Rubric() =>
        ExactSourceIdentity.TryCreate(
            "rubric_evaluation",
            Guid.Parse("22222222-2222-2222-2222-222222222206"),
            Guid.Parse("33333333-3333-3333-3333-333333333316"),
            RubricDigest).Value!;

    public static ExactSourceIdentity Submission() =>
        ExactSourceIdentity.TryCreate(
            "task_submission",
            Guid.Parse("22222222-2222-2222-2222-222222222209"),
            Guid.Parse("33333333-3333-3333-3333-333333333309"),
            SubmissionDigest).Value!;

    public static EvaluationDecision<FrozenInputIdentity> CreateFrozenInput() =>
        FrozenInputIdentity.TryCreate(
            "handoff.synthetic.0001",
            Ownership(),
            Guid.Parse("11111111-1111-4111-8111-111111111112"),
            "completed",
            42,
            "manifest-jcs-sha256-v2",
            TerminalSealDigest,
            Guid.Parse("11111111-1111-4111-8111-111111111113"),
            ConfigurationDigest,
            Guid.Parse("11111111-1111-4111-8111-111111111114"),
            ManifestDigest,
            Rubric(),
            Submission(),
            "evalreg.p0.v1",
            Model(),
            "lifecycle.activity-closure-365d.v1");

    public static EvaluationOwnership PerturbOwnership(string field)
    {
        var ownership = Ownership();
        return field switch
        {
            "organization" => ownership with { OrganizationId = Guid.NewGuid() },
            "activity" => ownership with { ActivityId = Guid.NewGuid() },
            "participant" => ownership with { ParticipantId = Guid.NewGuid() },
            "attempt" => ownership with { AttemptId = Guid.NewGuid() },
            "session" => ownership with { SessionId = Guid.NewGuid() },
            _ => throw new ArgumentOutOfRangeException(nameof(field), field, "Unknown ownership field."),
        };
    }

    public static EvaluationProcedureV1 LoadSyntheticProcedure()
    {
        var utf8 = LoadSyntheticProcedureUtf8();
        var resolved = EvaluationProcedureResolver.TryResolve(utf8);
        if (!resolved.Succeeded || resolved.Value is null)
        {
            throw new InvalidOperationException(resolved.OutcomeCode);
        }

        return resolved.Value;
    }

    public static byte[] LoadSyntheticProcedureUtf8() =>
        File.ReadAllBytes(
            Path.Combine(
                AppContext.BaseDirectory,
                "contracts",
                "fixtures",
                "schema",
                "v1",
                "evaluation",
                "evaluation-procedure",
                "valid-three-mode-synthetic.json"));

    public static byte[] LoadSyntheticProcedureCanonicalUtf8()
    {
        var repositoryRoot = FindRepositoryRoot();
        var hex = File.ReadAllText(
            Path.Combine(
                repositoryRoot,
                "contracts",
                "fixtures",
                "jcs",
                "evaluation-procedure-jcs-sha256-v1",
                "p0-text-synthetic",
                "canonical.utf8.hex")).Trim();
        return Convert.FromHexString(hex);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "FlexAgent.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }

    public static ExactSourceIdentity SyntheticProcedureRef() =>
        ExactSourceIdentity.TryCreate(
            "rubric_evaluation",
            Guid.Parse("22222222-2222-2222-2222-222222222206"),
            Guid.Parse("33333333-3333-3333-3333-333333333316"),
            "cb9db3a1cf6f96961cf8506ec3196edd2b9ce3d0dcf7cbfc4334ecaf624860de").Value!;

    public static VerifiedPermittedEvidenceMaterial VerifiedPermittedEvidenceItem(
        string evidenceStableId,
        Guid evidenceId,
        string sourceType = "submission.direct_text",
        string? contentDigest = null) =>
        new(
            evidenceStableId,
            evidenceId,
            sourceType,
            contentDigest ?? new string('b', 64));

    public static IReadOnlyList<VerifiedPermittedEvidenceMaterial> VerifiedPermittedEvidence(
        string evidenceStableId,
        Guid evidenceId,
        string sourceType = "submission.direct_text",
        string? contentDigest = null) =>
        [VerifiedPermittedEvidenceItem(evidenceStableId, evidenceId, sourceType, contentDigest)];
}
