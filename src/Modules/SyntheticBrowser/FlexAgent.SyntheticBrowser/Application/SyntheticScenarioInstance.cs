namespace FlexAgent.SyntheticBrowser.Application;

internal sealed class SyntheticScenarioInstance
{
    public SyntheticScenarioState State { get; } = new();
    public object Sync { get; } = new();
}
