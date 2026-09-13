namespace FlexAgent.Evaluation.Application;

public static class EvaluationAuthorizedActions
{
    public const string Admit = "evaluation.request.admit";
    public const string Claim = "evaluation.work.claim";
    public const string Execute = "evaluation.execute";
    public const string Complete = "evaluation.complete";
    public const string Retry = "evaluation.retry";
    public const string Annotate = "evaluation.annotate";
    public const string ReplacementSignal = "evaluation.replacement.signal";
    public const string StatusRead = "evaluation.status.read";
    public const string Read = "evaluation.read";
    public const string ReadEvidence = "evaluation.evidence.read";
}
