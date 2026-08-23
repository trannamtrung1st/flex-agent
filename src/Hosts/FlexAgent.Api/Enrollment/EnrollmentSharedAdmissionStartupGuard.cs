using FlexAgent.Submissions.Application;

namespace FlexAgent.Api;

public sealed class EnrollmentSharedAdmissionStartupGuard(
    IHostEnvironment environment,
    IEnrollmentSharedAdmissionPort admission) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!environment.IsProduction() && !environment.IsEnvironment("Staging"))
        {
            return;
        }

        if (!await admission.PolicyMatchesAsync(cancellationToken))
        {
            throw new InvalidOperationException(
                "Enrollment shared admission policy on this replica does not match the deployment-wide PostgreSQL policy.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
