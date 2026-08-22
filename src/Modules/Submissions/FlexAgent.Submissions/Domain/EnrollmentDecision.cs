namespace FlexAgent.Submissions.Domain;

public sealed record EnrollmentDecision<T>(
    bool Succeeded,
    string OutcomeCode,
    T? Value,
    string? Field = null)
{
    public static EnrollmentDecision<T> Ok(T value, string outcomeCode) =>
        new(true, outcomeCode, value);

    public static EnrollmentDecision<T> Fail(string outcomeCode, string? field = null) =>
        new(false, outcomeCode, default, field);
}
