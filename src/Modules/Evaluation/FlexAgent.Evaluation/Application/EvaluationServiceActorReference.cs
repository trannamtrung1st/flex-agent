namespace FlexAgent.Evaluation.Application;

public static class EvaluationServiceActorReference
{
    public static string Format(Guid serviceActorId)
    {
        if (serviceActorId == Guid.Empty)
        {
            throw new ArgumentOutOfRangeException(nameof(serviceActorId));
        }

        return $"service.{serviceActorId:N}";
    }
}
