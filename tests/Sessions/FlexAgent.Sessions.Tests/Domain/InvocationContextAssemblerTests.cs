using FlexAgent.Sessions.Domain;

namespace FlexAgent.Sessions.Tests.Domain;

public sealed class InvocationContextAssemblerTests
{
    [Fact]
    public void Assembled_context_contains_only_trusted_binding_and_visible_transcript_refs()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        session.AcceptParticipantMessage(
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);

        var context = InvocationContextAssembler.Assemble(session);

        Assert.Equal(session.Ownership, context.Ownership);
        Assert.Equal(session.Binding.ConfigurationDigest, context.ConfigurationDigest);
        Assert.Equal(session.Binding.Policy.PolicyDigest, context.PolicyDigest);
        Assert.Equal(session.Binding.PermittedSubmissionRefs, context.SubmissionRefs);
        Assert.Equal(session.Binding.PermittedKnowledgeRefs, context.KnowledgeRefs);
        Assert.Empty(context.MemoryReadRefs);
        Assert.Single(context.VisibleTranscript);
        Assert.Equal(TranscriptAuthorTypes.Participant, context.VisibleTranscript[0].AuthorType);
        Assert.Equal("msg.p.1", context.VisibleTranscript[0].MessageId);
        Assert.DoesNotContain(context.FactCategories, category => category == InvocationContextFactCategories.ModelControl);
        Assert.DoesNotContain(context.FactCategories, category => category == InvocationContextFactCategories.Credential);
    }

    [Fact]
    public void Offered_model_authored_control_facts_cannot_enter_invocation_context()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var offered = new[]
        {
            new InvocationContextFact(
                InvocationContextFactCategories.ModelControl,
                session.Ownership,
                "next_timer_request:PT5M"),
        };

        var result = InvocationContextAssembler.TryAssemble(session, offered);

        Assert.False(result.Succeeded);
        Assert.Equal(InvocationContextOutcomeCodes.DisallowedFact, result.OutcomeCode);
        Assert.Null(result.Context);
    }

    [Fact]
    public void Offered_foreign_ownership_facts_cannot_enter_invocation_context()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var foreign = session.Ownership with { SessionId = Guid.Parse("99999999-9999-9999-9999-999999999999") };
        var offered = new[]
        {
            new InvocationContextFact(
                InvocationContextFactCategories.TranscriptItem,
                foreign,
                "msg.other-session"),
        };

        var result = InvocationContextAssembler.TryAssemble(session, offered);

        Assert.False(result.Succeeded);
        Assert.Equal(InvocationContextOutcomeCodes.OwnershipMismatch, result.OutcomeCode);
    }

    [Fact]
    public void Offered_credential_facts_cannot_enter_invocation_context()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var offered = new[]
        {
            new InvocationContextFact(
                InvocationContextFactCategories.Credential,
                session.Ownership,
                "secret-ref"),
        };

        var result = InvocationContextAssembler.TryAssemble(session, offered);

        Assert.False(result.Succeeded);
        Assert.Equal(InvocationContextOutcomeCodes.DisallowedFact, result.OutcomeCode);
    }

    [Fact]
    public void Offered_permitted_submission_ref_is_accepted_as_already_bound()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var offered = new[]
        {
            new InvocationContextFact(
                InvocationContextFactCategories.SubmissionRef,
                session.Ownership,
                session.Binding.PermittedSubmissionRefs[0].ProtectedRef),
        };

        var result = InvocationContextAssembler.TryAssemble(session, offered);

        Assert.True(result.Succeeded, result.OutcomeCode);
        Assert.Contains(
            result.Context!.SubmissionRefs,
            reference => reference.ProtectedRef == session.Binding.PermittedSubmissionRefs[0].ProtectedRef);
    }

    [Fact]
    public void Unrelated_submission_ref_cannot_enter_invocation_context()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var offered = new[]
        {
            new InvocationContextFact(
                InvocationContextFactCategories.SubmissionRef,
                session.Ownership,
                "sub:other-activity"),
        };

        var result = InvocationContextAssembler.TryAssemble(session, offered);

        Assert.False(result.Succeeded);
        Assert.Equal(InvocationContextOutcomeCodes.UnpermittedReference, result.OutcomeCode);
    }

    [Fact]
    public void Offered_transcript_item_not_in_the_session_cannot_enter_invocation_context()
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        session.AcceptParticipantMessage(
            "msg.p.1", "turn.1", "slot.1", "trig.participant.1", "idem.p.1", SessionRuntimeTestFixtures.T0);
        var offered = new[]
        {
            new InvocationContextFact(
                InvocationContextFactCategories.TranscriptItem,
                session.Ownership,
                "msg.not-in-session"),
        };

        var result = InvocationContextAssembler.TryAssemble(session, offered);

        Assert.False(result.Succeeded);
        Assert.Equal(InvocationContextOutcomeCodes.UnpermittedReference, result.OutcomeCode);
    }
}
