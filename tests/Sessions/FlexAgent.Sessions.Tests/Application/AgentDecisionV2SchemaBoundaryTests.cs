using System.Text.Json;
using FlexAgent.Sessions.Application;
using FlexAgent.Sessions.Domain;
using FlexAgent.Sessions.Tests.Domain;

namespace FlexAgent.Sessions.Tests.Application;

public sealed class AgentDecisionV2SchemaBoundaryTests
{
    private static readonly string FixtureDirectory = Path.Combine(
        AppContext.BaseDirectory,
        "contracts",
        "fixtures",
        "schema",
        "v2",
        "session",
        "agent-decision");

    [Theory]
    [MemberData(nameof(InvalidV2FixtureFiles))]
    public async Task Invalid_v2_catalog_fixture_is_an_execution_failure_not_a_decision(string fileName)
    {
        var utf8Json = await File.ReadAllBytesAsync(
            Path.Combine(FixtureDirectory, fileName),
            TestContext.Current.CancellationToken);
        var invocationId = ReadInvocationId(utf8Json);
        var adapter = new DeterministicFakeModelExecutionAdapter();
        adapter.EnqueueControlJson(utf8Json);

        var result = await adapter.ExecuteAsync(
            CreateRequest(invocationId),
            TestContext.Current.CancellationToken);

        Assert.IsType<ModelExecutionFailed>(result);
        Assert.Equal(
            ExecutionFailureReasons.MalformedControl,
            Assert.IsType<ModelExecutionFailed>(result).ReasonCategory);
    }

    [Theory]
    [MemberData(nameof(ValidV2FixtureFiles))]
    public async Task Valid_v2_catalog_fixture_yields_structured_control(string fileName)
    {
        var utf8Json = await File.ReadAllBytesAsync(
            Path.Combine(FixtureDirectory, fileName),
            TestContext.Current.CancellationToken);
        var invocationId = ReadInvocationId(utf8Json);
        var adapter = new DeterministicFakeModelExecutionAdapter();
        adapter.EnqueueControlJson(utf8Json);

        var result = await adapter.ExecuteAsync(
            CreateRequest(invocationId),
            TestContext.Current.CancellationToken);

        var control = Assert.IsType<ModelExecutionStructuredControl>(result);
        Assert.Equal(invocationId, control.Envelope.InvocationId);
        Assert.Same(control.Envelope, control.Control.Envelope);
    }

    [Fact]
    public void Invalid_timer_duration_is_schema_invalid_even_when_the_handwritten_parser_accepts_it()
    {
        var utf8Json = File.ReadAllBytes(Path.Combine(FixtureDirectory, "invalid-timer-duration.json"));
        var parsed = AgentDecisionEnvelopeParser.Parse(utf8Json);
        Assert.True(parsed.Succeeded, "Handwritten mapping is not the schema authority.");

        var boundary = AgentDecisionEnvelopeReader.Read(utf8Json);
        Assert.False(boundary.Succeeded);
        Assert.Equal(ExecutionFailureReasons.MalformedControl, boundary.FailureReasonCategory);
        Assert.Null(boundary.Envelope);
    }

    public static TheoryData<string> InvalidV2FixtureFiles() => Discover("invalid-");

    public static TheoryData<string> ValidV2FixtureFiles() => Discover("valid-");

    private static TheoryData<string> Discover(string prefix)
    {
        var data = new TheoryData<string>();
        foreach (var path in Directory.GetFiles(FixtureDirectory, $"{prefix}*.json").OrderBy(path => path, StringComparer.Ordinal))
        {
            data.Add(Path.GetFileName(path));
        }

        return data;
    }

    private static string ReadInvocationId(byte[] utf8Json)
    {
        using var document = JsonDocument.Parse(utf8Json);
        return document.RootElement.GetProperty("agent_invocation_id").GetString()
            ?? throw new InvalidOperationException("Fixture is missing agent_invocation_id.");
    }

    private static ModelExecutionAttemptRequest CreateRequest(string invocationId)
    {
        var session = SessionRuntimeTestFixtures.CreateActiveSession();
        var context = new InvocationContext(
            session.Ownership,
            session.Binding.ConfigurationDigest,
            session.Policy.PolicyDigest,
            session.Binding.PermittedSubmissionRefs,
            session.Binding.PermittedKnowledgeRefs,
            session.Binding.PermittedMemoryReadRefs,
            [],
            [InvocationContextFactCategories.SubmissionRef]);
        return new ModelExecutionAttemptRequest(
            session.Ownership,
            invocationId,
            "synthetic.provider",
            "bind.opaque.0001",
            "bind.v1",
            context,
            AttemptOrdinal: 1,
            MaxControlUtf8Bytes: 65_536);
    }
}
