using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Application;

/// <summary>
/// Frozen synthetic-development model identity shared by Evaluation fixtures and
/// the Development/Testing synthetic adapter. Do not change these fields without
/// also rotating already-frozen Evaluation model bindings.
/// </summary>
public static class EvaluationSyntheticDevelopmentModelProfile
{
    public const string ProfileId = "mdl.p0.text.synthetic";
    public const string ProfileVersion = "mdl.p0.text.synthetic.v1";
    public const string ProviderId = "provider.synthetic";
    public const string CredentialMode = "organization_byok";
    public const string CredentialBindingReference = "cred.bind.synthetic";
    public const string CredentialBindingVersion = "cred.bind.synthetic.v1";
    public const string SecretName = "synthetic.eval.dev";

    public static bool MatchesFrozenModel(FrozenModelIdentity model)
    {
        ArgumentNullException.ThrowIfNull(model);
        return string.Equals(model.ProfileId, ProfileId, StringComparison.Ordinal)
            && string.Equals(model.ProfileVersion, ProfileVersion, StringComparison.Ordinal)
            && string.Equals(model.ProviderId, ProviderId, StringComparison.Ordinal)
            && string.Equals(model.CredentialMode, CredentialMode, StringComparison.Ordinal)
            && string.Equals(model.CredentialBindingReference, CredentialBindingReference, StringComparison.Ordinal)
            && string.Equals(model.CredentialBindingVersion, CredentialBindingVersion, StringComparison.Ordinal);
    }

    /// <summary>
    /// Catalog row for compose-time binding admission. <see cref="Guid.Empty"/> owner
    /// is the synthetic-development wildcard: any organization may resolve this binding.
    /// </summary>
    public static EvaluationModelCredentialCatalogRecord CreateCatalogRecord() =>
        new(
            CredentialBindingReference,
            CredentialBindingVersion,
            Guid.Empty,
            ProviderId,
            CredentialMode,
            Revoked: false,
            SecretName);
}

public sealed record EvaluationModelCredentialCatalogRecord(
    string BindingReference,
    string BindingVersion,
    Guid OrganizationId,
    string ProviderId,
    string CredentialMode,
    bool Revoked,
    string SecretName);

public interface IEvaluationModelCredentialCatalog
{
    EvaluationModelCredentialCatalogRecord? TryGet(string bindingReference, string bindingVersion);
}

public sealed class InMemoryEvaluationModelCredentialCatalog : IEvaluationModelCredentialCatalog
{
    private readonly Dictionary<(string Reference, string Version), EvaluationModelCredentialCatalogRecord> _records;

    public InMemoryEvaluationModelCredentialCatalog(params EvaluationModelCredentialCatalogRecord[] records)
    {
        ArgumentNullException.ThrowIfNull(records);
        _records = new Dictionary<(string, string), EvaluationModelCredentialCatalogRecord>();
        foreach (var record in records)
        {
            _records[(record.BindingReference, record.BindingVersion)] = record;
        }
    }

    public EvaluationModelCredentialCatalogRecord? TryGet(string bindingReference, string bindingVersion)
    {
        if (string.IsNullOrWhiteSpace(bindingReference) || string.IsNullOrWhiteSpace(bindingVersion))
        {
            return null;
        }

        return _records.TryGetValue((bindingReference, bindingVersion), out var record)
            ? record
            : null;
    }
}

public static class EvaluationModelCredentialBindingAdmission
{
    public static bool TryAdmit(
        Guid organizationId,
        FrozenModelIdentity model,
        IEvaluationModelCredentialCatalog catalog,
        out EvaluationModelCredentialCatalogRecord? admittedRecord)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(catalog);
        admittedRecord = null;

        if (organizationId == Guid.Empty
            || !EvaluationSyntheticDevelopmentModelProfile.MatchesFrozenModel(model))
        {
            return false;
        }

        var record = catalog.TryGet(model.CredentialBindingReference, model.CredentialBindingVersion);
        if (record is null
            || record.Revoked
            || !string.Equals(record.BindingReference, model.CredentialBindingReference, StringComparison.Ordinal)
            || !string.Equals(record.BindingVersion, model.CredentialBindingVersion, StringComparison.Ordinal)
            || !string.Equals(record.ProviderId, model.ProviderId, StringComparison.Ordinal)
            || !string.Equals(record.CredentialMode, model.CredentialMode, StringComparison.Ordinal)
            || (record.OrganizationId != Guid.Empty && record.OrganizationId != organizationId))
        {
            return false;
        }

        admittedRecord = record;
        return true;
    }
}

public sealed record EvaluationModelExecutionCompositionRequest(
    string Adapter,
    bool Qualified,
    string EnvironmentName,
    bool WorkloadIdentityVerified,
    string WorkloadIdentityProfile,
    FrozenModelIdentity FrozenModel,
    Guid OrganizationId,
    IEvaluationModelCredentialCatalog? CredentialCatalog = null);

public sealed record EvaluationModelExecutionComposition(
    IEvaluationModelExecutionPort Port,
    IEvaluationModelCredentialCatalog CredentialCatalog,
    string Adapter,
    bool Qualified,
    string QualificationScope = "")
{
    public static EvaluationModelExecutionComposition FailClosed(
        string adapter = EvaluationModelAdapterKinds.FailClosed,
        string qualificationScope = "") =>
        new(
            new FailClosedEvaluationModelExecutionPort(),
            new InMemoryEvaluationModelCredentialCatalog(),
            string.IsNullOrWhiteSpace(adapter) ? EvaluationModelAdapterKinds.FailClosed : adapter,
            false,
            qualificationScope);
}

public static class EvaluationModelExecutionCompositionComposer
{
    public static EvaluationModelExecutionComposition Compose(
        EvaluationModelExecutionCompositionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var adapter = string.IsNullOrWhiteSpace(request.Adapter)
            ? EvaluationModelAdapterKinds.FailClosed
            : request.Adapter;

        if (string.Equals(adapter, EvaluationModelAdapterKinds.FailClosed, StringComparison.Ordinal))
        {
            return EvaluationModelExecutionComposition.FailClosed(adapter);
        }

        if (string.Equals(adapter, EvaluationModelAdapterKinds.SyntheticDevelopment, StringComparison.Ordinal))
        {
            return ComposeSyntheticDevelopment(request);
        }

        return EvaluationModelExecutionComposition.FailClosed(adapter);
    }

    private static EvaluationModelExecutionComposition ComposeSyntheticDevelopment(
        EvaluationModelExecutionCompositionRequest request)
    {
        if (!IsSyntheticHostProfile(request.EnvironmentName))
        {
            return EvaluationModelExecutionComposition.FailClosed(
                EvaluationModelAdapterKinds.SyntheticDevelopment);
        }

        if (!request.Qualified)
        {
            return EvaluationModelExecutionComposition.FailClosed(
                EvaluationModelAdapterKinds.SyntheticDevelopment);
        }

        if (!request.WorkloadIdentityVerified
            || !string.Equals(
                request.WorkloadIdentityProfile,
                EvaluationWorkloadIdentityProfiles.SyntheticConfiguredActor,
                StringComparison.Ordinal))
        {
            return EvaluationModelExecutionComposition.FailClosed(
                EvaluationModelAdapterKinds.SyntheticDevelopment);
        }

        if (!EvaluationSyntheticDevelopmentModelProfile.MatchesFrozenModel(request.FrozenModel))
        {
            return EvaluationModelExecutionComposition.FailClosed(
                EvaluationModelAdapterKinds.SyntheticDevelopment);
        }

        var catalog = request.CredentialCatalog
            ?? new InMemoryEvaluationModelCredentialCatalog(
                EvaluationSyntheticDevelopmentModelProfile.CreateCatalogRecord());

        if (!EvaluationModelCredentialBindingAdmission.TryAdmit(
                request.OrganizationId,
                request.FrozenModel,
                catalog,
                out _))
        {
            return EvaluationModelExecutionComposition.FailClosed(
                EvaluationModelAdapterKinds.SyntheticDevelopment);
        }

        return new EvaluationModelExecutionComposition(
            new SyntheticEvaluationModelExecutionAdapter(),
            catalog,
            EvaluationModelAdapterKinds.SyntheticDevelopment,
            true);
    }

    private static bool IsSyntheticHostProfile(string environmentName) =>
        string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase)
        || string.Equals(environmentName, "Testing", StringComparison.OrdinalIgnoreCase);
}
