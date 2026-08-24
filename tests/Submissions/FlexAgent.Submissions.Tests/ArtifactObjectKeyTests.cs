using FlexAgent.Submissions.Application;

namespace FlexAgent.Submissions.Tests;

public sealed class ArtifactObjectKeyTests
{
    [Fact]
    public void Create_embeds_organization_scope_in_key()
    {
        var organizationId = Guid.Parse("cccccccc-cccc-4ccc-8ccc-cccccccccccc");
        var artifactId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1");
        var key = ArtifactObjectKey.Create(organizationId, artifactId);
        Assert.Equal("org/cccccccc-cccc-4ccc-8ccc-cccccccccccc/aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1", key.Value);
        Assert.Equal(organizationId, key.ScopedOrganizationId);
        Assert.True(key.BelongsToOrganization(organizationId));
        Assert.False(key.BelongsToOrganization(Guid.NewGuid()));
    }
}
