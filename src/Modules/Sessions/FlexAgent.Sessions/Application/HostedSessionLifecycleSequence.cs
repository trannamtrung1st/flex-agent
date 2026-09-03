using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Application;

public static class HostedSessionLifecycleSequence
{
    public static IReadOnlyList<string> Transitions(string commandType) =>
        commandType switch
        {
            "session.pause.v1" => [SessionLifecycleTransitions.Pause],
            "session.resume.v1" => [SessionLifecycleTransitions.Resume],
            "session.complete.v1" =>
            [
                SessionLifecycleTransitions.BeginCompleting,
                SessionLifecycleTransitions.Complete,
            ],
            "session.terminate.v1" =>
            [
                SessionLifecycleTransitions.BeginCompleting,
                SessionLifecycleTransitions.Terminate,
            ],
            _ => [],
        };
}
