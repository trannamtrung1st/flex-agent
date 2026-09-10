using FlexAgent.Evaluation.Application;
using FlexAgent.Evaluation.Infrastructure;

namespace FlexAgent.Worker;

internal static class WorkerEvaluationLane
{
    public static bool ResolveRequested(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var evaluationProcessingRequested = configuration.GetValue("Evaluation:Processing:Enabled", false);
        if (evaluationProcessingRequested && !EvaluationInfrastructure.ProcessingEnabled)
        {
            throw new InvalidOperationException(
                "Evaluation:Processing:Enabled requires EvaluationInfrastructure.ProcessingEnabled to be true.");
        }

        return evaluationProcessingRequested;
    }

    public static void RegisterServices(IServiceCollection services, bool evaluationProcessingEnabled)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (evaluationProcessingEnabled)
        {
            throw new InvalidOperationException(
                "Evaluation durable work processing is not implemented.");
        }

        services.AddSingleton<IEvaluationDurableWorkProcessor, IdleEvaluationDurableWorkProcessor>();
    }
}
