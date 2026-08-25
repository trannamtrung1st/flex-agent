using FlexAgent.Api;
using FlexAgent.Contracts.Enrollment;
using FlexAgent.Submissions.Application;

namespace FlexAgent.Runtime.Tests;

public sealed class EnrollmentRequestValidatorTests
{
    [Fact]
    public void Assign_validator_rejects_empty_participant()
    {
        Assert.False(EnrollmentAssignRequestValidator.IsValid(
            new EnrollmentAssignCommandV1("v1", Guid.Empty, "enroll-assign-synthetic-0001")));
    }

    [Fact]
    public void Assign_validator_accepts_valid_command()
    {
        Assert.True(EnrollmentAssignRequestValidator.IsValid(
            new EnrollmentAssignCommandV1("v1", Guid.CreateVersion7(), "enroll-assign-synthetic-0001")));
    }

    [Fact]
    public void Lifecycle_validator_rejects_blank_reason()
    {
        Assert.False(EnrollmentLifecycleRequestValidator.IsValid(
            new EnrollmentLifecycleCommandV1("v1", "  ", 1, "enroll-life-synthetic-0001")));
    }

    [Fact]
    public void Grant_accommodation_validator_rejects_missing_dimension()
    {
        Assert.False(GrantAccommodationRequestValidator.IsValid(
            new GrantAccommodationCommandV2(
                "v2",
                "",
                "1.5x",
                "medical",
                null,
                false,
                1,
                "accom-grant-synthetic-0001")));
    }
}
