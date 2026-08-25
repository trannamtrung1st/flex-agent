using FlexAgent.Contracts.Submission;
using FlexAgent.Submissions.Application;
using FlexAgent.Submissions.Domain;

namespace FlexAgent.Api;

internal static class SubmissionResponseMapper
{
    public static IntakeMutationOutcomeV2 MapOutcome(IntakeMutationOutcome outcome) =>
        new(
            "v2",
            outcome.Succeeded,
            outcome.OutcomeCode,
            outcome.IntakeId,
            outcome.SubmissionId,
            outcome.Status,
            outcome.Revision,
            outcome.VersionId,
            outcome.VersionNumber,
            MapActions(outcome.Status, outcome.VersionNumber is not null));

    public static IReadOnlyList<string> MapActions(string? status, bool hasVersion) =>
        SubmissionLifecycle.PermittedActions(
            true,
            status,
            hasVersion);

    public static MyWorkSubmissionV2 MapMyWork(MyWorkSubmissionProjection projection) =>
        new(
            "v2",
            projection.EnrollmentId,
            projection.EnrollmentStatus,
            projection.IntakeAvailable,
            projection.UnavailableReason,
            projection.Requirements is null ? null : MapRequirements(projection.Requirements),
            projection.ActiveIntake is null ? null : MapIntake(projection.ActiveIntake),
            projection.VersionHistory.Select(version => new AcceptedVersionSummaryV2(
                version.VersionId,
                version.VersionNumber,
                EnrollmentEndpointExtensions.FormatUtc(version.AcceptedAtUtc)!,
                version.ItemCount)).ToArray(),
            projection.PermittedActions);

    public static MaterialRequirementsV2 MapRequirements(NormalizedMaterialPolicy policy) =>
        new(
            policy.ContractVersion,
            policy.MaxAttachmentCount,
            policy.MaxAttachmentAggregateBytes,
            policy.Categories.FirstOrDefault(category => category.Category == MaterialCategories.DirectText)?.MaxBytes ?? 1_048_576,
            policy.ScannerMode == MaterialScannerMode.Required ? "required" : "disabled_by_approved_policy",
            policy.Categories.Select(category => new MaterialCategoryLimitV2(
                category.Category,
                category.Available,
                category.MaxBytes)).ToArray());

    public static SubmissionIntakeV2 MapIntake(SubmissionIntakeProjection intake) =>
        new(
            intake.IntakeId,
            intake.SubmissionId,
            intake.Status,
            intake.Revision,
            EnrollmentEndpointExtensions.FormatUtc(intake.CreatedAtUtc)!,
            EnrollmentEndpointExtensions.FormatUtc(intake.UpdatedAtUtc)!,
            EnrollmentEndpointExtensions.FormatUtc(intake.CompleteReceiptAtUtc),
            intake.Items.Select(item => new SubmissionIntakeItemV2(
                item.ItemId,
                item.Category,
                item.Filename,
                item.ByteCount,
                item.ReceiptState)).ToArray(),
            intake.PermittedActions);

    public static AcceptedVersionDetailV2 MapAcceptedVersion(AcceptedVersionDetail detail) =>
        new(
            "v2",
            detail.Summary.VersionId,
            detail.Summary.VersionNumber,
            EnrollmentEndpointExtensions.FormatUtc(detail.Summary.AcceptedAtUtc)!,
            detail.Items.Select(item => new AcceptedVersionItemV2(
                item.ItemId,
                item.Category,
                item.Filename,
                item.ByteCount,
                item.PreviewAuthorized,
                item.DownloadAuthorized)).ToArray(),
            detail.PermittedActions);
}
