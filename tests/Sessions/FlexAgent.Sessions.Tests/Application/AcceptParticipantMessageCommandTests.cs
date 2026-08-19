using System.Reflection;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.Tests.Domain;

namespace FlexAgent.Sessions.Tests.Application;

public sealed class AcceptParticipantMessageCommandTests
{
    [Fact]
    public void Command_requires_non_optional_exact_utf8_text()
    {
        var ctor = typeof(AcceptParticipantMessageCommand).GetConstructors(BindingFlags.Public | BindingFlags.Instance).Single();
        var text = ctor.GetParameters().Single(parameter => parameter.Name == "ExactUtf8Text");
        Assert.Equal(typeof(string), text.ParameterType);
        Assert.False(text.HasDefaultValue);
        Assert.False(text.IsOptional);
    }

    [Fact]
    public void Handler_rejects_blank_exact_text()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var result = new AcceptParticipantMessageHandler().Handle(
            new AcceptParticipantMessageCommand(
                SessionRuntimeTestFixtures.CreateActor(),
                session.Ownership,
                ExpectedSessionVersion: 0,
                "msg.p.omit",
                "turn.omit",
                "slot.omit",
                "trig.participant.omit",
                "idem.p.omit",
                Guid.NewGuid(),
                "unit.test",
                "  "),
            session,
            SessionRuntimeTestFixtures.T0);

        Assert.False(result.Succeeded);
        Assert.Equal(TriggerAdmissionOutcomeCodes.MissingParticipantContent, result.OutcomeCode);
        Assert.Empty(session.VisibleTranscript);
    }
}
