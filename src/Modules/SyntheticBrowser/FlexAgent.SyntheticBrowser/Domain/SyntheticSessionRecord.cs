namespace FlexAgent.SyntheticBrowser.Domain;

public sealed class SyntheticSessionRecord
{
    public required string SessionId { get; init; }
    public required string ScenarioId { get; init; }
    public required string ScenarioInstanceId { get; init; }
    public required string ActorStage { get; init; }
    public required string ActorId { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
}

public sealed class ScenarioGrantRecord
{
    public required string GrantToken { get; init; }
    public required string ScenarioId { get; init; }
    public required string ScenarioInstanceId { get; init; }
    public required string ActorStage { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
}
