using FlexAgent.Contracts.Evaluation;

namespace FlexAgent.Evaluation.Domain;

public static class EvaluationModelResponseDocumentReader
{
    public static EvaluationModelResponseReadResult Read(ReadOnlySpan<byte> utf8Json)
    {
        if (utf8Json.IsEmpty)
        {
            return EvaluationModelResponseReadResult.Fail("model_response");
        }

        if (!EvaluationModelResponseSchemaValidator.IsSchemaValid(utf8Json))
        {
            return EvaluationModelResponseReadResult.Fail("model_response");
        }

        if (!EvaluationModelResponseDocumentParser.TryParse(utf8Json, out var response, out _))
        {
            return EvaluationModelResponseReadResult.Fail("model_response");
        }

        return EvaluationModelResponseReadResult.Ok(response);
    }
}

public readonly record struct EvaluationModelResponseReadResult(
    bool Succeeded,
    EvaluationModelResponseV1? Value,
    string? Field)
{
    public static EvaluationModelResponseReadResult Ok(EvaluationModelResponseV1 value) =>
        new(true, value, null);

    public static EvaluationModelResponseReadResult Fail(string field) =>
        new(false, null, field);
}
