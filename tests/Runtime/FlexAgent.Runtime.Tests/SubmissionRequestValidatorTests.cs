using FlexAgent.Api;
using FlexAgent.Contracts.Submission;
using FlexAgent.Submissions.Application;

namespace FlexAgent.Runtime.Tests;

public sealed class SubmissionRequestValidatorTests
{
    [Fact]
    public void Begin_intake_validator_rejects_invalid_schema_version()
    {
        Assert.False(BeginIntakeRequestValidator.IsValid(new BeginIntakeCommandV2("v1", "intake-begin-synthetic-0001")));
    }

    [Fact]
    public void Begin_intake_validator_rejects_invalid_idempotency_key()
    {
        Assert.False(BeginIntakeRequestValidator.IsValid(new BeginIntakeCommandV2("v2", "")));
    }

    [Fact]
    public void Begin_intake_validator_accepts_valid_command()
    {
        Assert.True(BeginIntakeRequestValidator.IsValid(new BeginIntakeCommandV2("v2", "intake-begin-synthetic-0001")));
    }

    [Fact]
    public void Start_attempt_validator_rejects_short_digest()
    {
        Assert.False(AttemptRequestValidators.IsValid(new StartAttemptCommandV2("v2", "attempt-start-synthetic-0001", "abc")));
    }

    [Fact]
    public void Start_attempt_validator_rejects_uppercase_digest()
    {
        Assert.False(AttemptRequestValidators.IsValid(new StartAttemptCommandV2(
            "v2",
            "attempt-start-synthetic-0001",
            new string('A', 64))));
    }

    [Fact]
    public void Start_attempt_validator_accepts_valid_command()
    {
        Assert.True(AttemptRequestValidators.IsValid(new StartAttemptCommandV2(
            "v2",
            "attempt-start-synthetic-0001",
            new string('a', 64))));
    }

    [Fact]
    public void Complete_item_validator_rejects_empty_content()
    {
        Assert.False(CompleteIntakeItemRequestValidator.IsValid(
            new CompleteIntakeItemCommandV2("v2", "direct_text", "notes.txt", "text/plain", "", 1, "item-complete-synthetic-0001")));
    }

    [Fact]
    public void Intake_revision_validator_rejects_invalid_revision()
    {
        Assert.False(IntakeRevisionRequestValidator.IsValid(
            new IntakeRevisionCommandV2("v2", 0, "intake-cancel-synthetic-0001")));
    }

    [Fact]
    public void Intake_revision_validator_accepts_valid_command()
    {
        Assert.True(IntakeRevisionRequestValidator.IsValid(
            new IntakeRevisionCommandV2("v2", 1, "intake-cancel-synthetic-0001")));
    }
}
