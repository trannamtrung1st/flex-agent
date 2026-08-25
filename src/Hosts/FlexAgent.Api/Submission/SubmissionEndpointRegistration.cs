using FlexAgent.Submissions.Application;

namespace FlexAgent.Api;

public static partial class SubmissionEndpointExtensions
{
    public static IEndpointRouteBuilder MapSubmissionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        if (endpoints.ServiceProvider.GetService<IIntakeCoordinator>() is null)
        {
            return endpoints;
        }

        var group = endpoints.MapGroup("/v2/assessment");
        group.MapGet("/my-work/{enrollmentId:guid}/submission", GetMyWorkSubmission);
        group.MapGet("/my-work/{enrollmentId:guid}/submission/intake/{intakeId:guid}", GetIntake);
        group.MapGet("/my-work/{enrollmentId:guid}/submission/versions/{versionId:guid}", GetAcceptedVersion);
        group.MapGet("/my-work/{enrollmentId:guid}/submission/versions/{versionId:guid}/items/{itemId:guid}/preview", GetItemPreview);
        group.MapGet("/my-work/{enrollmentId:guid}/submission/versions/{versionId:guid}/items/{itemId:guid}/download", GetItemDownload);
        group.MapPost("/my-work/{enrollmentId:guid}/submission/intake", BeginIntake);
        group.MapPost("/my-work/{enrollmentId:guid}/submission/intake/{intakeId:guid}/items", CompleteItem);
        group.MapPost("/my-work/{enrollmentId:guid}/submission/intake/{intakeId:guid}/cancel", CancelIntake);
        group.MapPost("/my-work/{enrollmentId:guid}/submission/intake/{intakeId:guid}/finalize", FinalizeIntake);
        return endpoints;
    }
}
