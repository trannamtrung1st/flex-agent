namespace FlexAgent.Evaluation.Domain;

public sealed record EvaluationOwnership(
    Guid OrganizationId,
    Guid ActivityId,
    Guid ParticipantId,
    Guid AttemptId,
    Guid SessionId)
{
    public static EvaluationDecision<EvaluationOwnership> TryCreate(
        Guid organizationId,
        Guid activityId,
        Guid participantId,
        Guid attemptId,
        Guid sessionId)
    {
        if (organizationId == Guid.Empty
            || activityId == Guid.Empty
            || participantId == Guid.Empty
            || attemptId == Guid.Empty
            || sessionId == Guid.Empty)
        {
            return EvaluationDecision<EvaluationOwnership>.Fail(EvaluationFailureCodes.IncompleteOwnership);
        }

        return EvaluationDecision<EvaluationOwnership>.Ok(
            new EvaluationOwnership(organizationId, activityId, participantId, attemptId, sessionId));
    }
}

public sealed record ExactSourceIdentity(
    string SourceKey,
    Guid SourceId,
    Guid SourceVersionId,
    string ContentDigest)
{
    public static EvaluationDecision<ExactSourceIdentity> TryCreate(
        string sourceKey,
        Guid sourceId,
        Guid sourceVersionId,
        string contentDigest)
    {
        if (!EvaluationIdentity.IsStableId(sourceKey)
            || EvaluationIdentity.ContainsMutableAlias(sourceKey)
            || sourceId == Guid.Empty
            || sourceVersionId == Guid.Empty
            || !EvaluationIdentity.IsSha256Hex(contentDigest))
        {
            return EvaluationDecision<ExactSourceIdentity>.Fail(
                EvaluationIdentity.ContainsMutableAlias(sourceKey)
                    ? EvaluationFailureCodes.MutableAlias
                    : EvaluationFailureCodes.InvalidField);
        }

        return EvaluationDecision<ExactSourceIdentity>.Ok(
            new ExactSourceIdentity(sourceKey, sourceId, sourceVersionId, contentDigest));
    }
}

public sealed record FrozenModelIdentity(
    string ProfileId,
    string ProfileVersion,
    string ProfileDigest,
    string ProviderId,
    string CredentialMode,
    string CredentialBindingReference,
    string CredentialBindingVersion)
{
    public static EvaluationDecision<FrozenModelIdentity> TryCreate(
        string profileId,
        string profileVersion,
        string profileDigest,
        string providerId,
        string credentialMode,
        string credentialBindingReference,
        string credentialBindingVersion)
    {
        if (!EvaluationIdentity.IsStableId(profileId)
            || !EvaluationIdentity.IsStableId(profileVersion)
            || !EvaluationIdentity.IsSha256Hex(profileDigest)
            || !EvaluationIdentity.IsStableId(providerId)
            || !EvaluationIdentity.IsStableId(credentialMode)
            || !EvaluationIdentity.IsStableId(credentialBindingReference)
            || !EvaluationIdentity.IsStableId(credentialBindingVersion)
            || EvaluationIdentity.ContainsMutableAlias(profileId)
            || EvaluationIdentity.ContainsMutableAlias(profileVersion)
            || EvaluationIdentity.ContainsMutableAlias(providerId)
            || EvaluationIdentity.ContainsMutableAlias(credentialBindingReference)
            || EvaluationIdentity.ContainsMutableAlias(credentialBindingVersion))
        {
            return EvaluationDecision<FrozenModelIdentity>.Fail(EvaluationFailureCodes.UnqualifiedModel);
        }

        return EvaluationDecision<FrozenModelIdentity>.Ok(
            new FrozenModelIdentity(
                profileId,
                profileVersion,
                profileDigest,
                providerId,
                credentialMode,
                credentialBindingReference,
                credentialBindingVersion));
    }
}

public sealed record FrozenInputIdentity(
    Guid HandoffId,
    EvaluationOwnership Ownership,
    string ManifestSealProcedureId,
    string ConfigurationDigest,
    string ManifestDigest,
    ExactSourceIdentity Rubric,
    ExactSourceIdentity Submission,
    string EvaluatorRegistryVersion,
    FrozenModelIdentity Model,
    string LifecyclePolicyRef)
{
    public static EvaluationDecision<FrozenInputIdentity> TryCreate(
        Guid handoffId,
        EvaluationOwnership ownership,
        string manifestSealProcedureId,
        string configurationDigest,
        string manifestDigest,
        ExactSourceIdentity rubric,
        ExactSourceIdentity submission,
        string evaluatorRegistryVersion,
        FrozenModelIdentity model,
        string lifecyclePolicyRef)
    {
        if (handoffId == Guid.Empty
            || (manifestSealProcedureId is not ("manifest-jcs-sha256-v1" or "manifest-jcs-sha256-v2"))
            || !EvaluationIdentity.IsSha256Hex(configurationDigest)
            || !EvaluationIdentity.IsSha256Hex(manifestDigest)
            || !EvaluationIdentity.IsStableId(evaluatorRegistryVersion)
            || !EvaluationIdentity.IsStableId(lifecyclePolicyRef))
        {
            return EvaluationDecision<FrozenInputIdentity>.Fail(EvaluationFailureCodes.InvalidField);
        }

        if (!string.Equals(rubric.SourceKey, "rubric_evaluation", StringComparison.Ordinal))
        {
            return EvaluationDecision<FrozenInputIdentity>.Fail(EvaluationFailureCodes.InvalidField, "rubric");
        }

        if (EvaluationIdentity.ContainsMutableAlias(evaluatorRegistryVersion)
            || EvaluationIdentity.ContainsMutableAlias(lifecyclePolicyRef)
            || EvaluationIdentity.ContainsMutableAlias(submission.SourceKey))
        {
            return EvaluationDecision<FrozenInputIdentity>.Fail(EvaluationFailureCodes.MutableAlias);
        }

        return EvaluationDecision<FrozenInputIdentity>.Ok(
            new FrozenInputIdentity(
                handoffId,
                ownership,
                manifestSealProcedureId,
                configurationDigest,
                manifestDigest,
                rubric,
                submission,
                evaluatorRegistryVersion,
                model,
                lifecyclePolicyRef));
    }
}
