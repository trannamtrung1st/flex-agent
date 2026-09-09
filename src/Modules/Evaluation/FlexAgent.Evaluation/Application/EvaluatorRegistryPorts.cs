using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Application;

public interface IEvaluatorRegistry
{
    EvaluationDecision<EvaluatorRegistrySnapshot> TryGetRegistry(string registryVersion);
}
