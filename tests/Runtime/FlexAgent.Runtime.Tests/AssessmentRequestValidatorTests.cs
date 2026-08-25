using FlexAgent.Api;
using FlexAgent.AssessmentConfiguration.Application;

namespace FlexAgent.Runtime.Tests;

public sealed class AssessmentRequestValidatorTests
{
    [Fact]
    public void Activate_validator_rejects_empty_revision_id()
    {
        Assert.False(AssessmentActivateRequestValidator.IsValid(Guid.Empty, 1));
    }

    [Fact]
    public void Activate_validator_accepts_valid_revision()
    {
        Assert.True(AssessmentActivateRequestValidator.IsValid(Guid.CreateVersion7(), 1));
    }

    [Fact]
    public void Reconcile_validator_rejects_blank_idempotency_key()
    {
        Assert.False(AssessmentReconcileQueryValidator.IsValid(" "));
    }

    [Fact]
    public void Reconcile_validator_accepts_valid_key()
    {
        Assert.True(AssessmentReconcileQueryValidator.IsValid("activation-synthetic-0001"));
    }
}
