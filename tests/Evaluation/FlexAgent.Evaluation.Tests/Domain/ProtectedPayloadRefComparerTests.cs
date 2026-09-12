using FlexAgent.Contracts.Manifest;
using FlexAgent.Evaluation.Domain;

namespace FlexAgent.Evaluation.Tests.Domain;

public sealed class ProtectedPayloadRefComparerTests
{
    [Fact]
    public void Matching_refs_are_equal()
    {
        var left = new ProtectedPayloadRefV1("prot.eval.res.example", new string('a', 64));
        var right = new ProtectedPayloadRefV1("prot.eval.res.example", new string('a', 64));

        Assert.True(ProtectedPayloadRefComparer.Matches(left, right));
    }

    [Fact]
    public void Different_protected_ref_is_not_equal()
    {
        var left = new ProtectedPayloadRefV1("prot.eval.res.left", new string('a', 64));
        var right = new ProtectedPayloadRefV1("prot.eval.res.right", new string('a', 64));

        Assert.False(ProtectedPayloadRefComparer.Matches(left, right));
    }

    [Fact]
    public void Different_content_digest_is_not_equal()
    {
        var left = new ProtectedPayloadRefV1("prot.eval.res.example", new string('a', 64));
        var right = new ProtectedPayloadRefV1("prot.eval.res.example", new string('b', 64));

        Assert.False(ProtectedPayloadRefComparer.Matches(left, right));
    }
}
