namespace FlexAgent.Sessions.Domain;

public static class InvocationContextAssembler
{
    private static readonly HashSet<string> DisallowedCategories =
    [
        InvocationContextFactCategories.ModelControl,
        InvocationContextFactCategories.Credential,
    ];

    public static InvocationContext Assemble(SessionRuntime session)
    {
        ArgumentNullException.ThrowIfNull(session);
        return CreateContext(session);
    }

    public static InvocationContextAssembleResult TryAssemble(
        SessionRuntime session,
        IReadOnlyList<InvocationContextFact> offeredFacts)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(offeredFacts);

        foreach (var fact in offeredFacts)
        {
            if (DisallowedCategories.Contains(fact.Category))
            {
                return new InvocationContextAssembleResult(
                    false,
                    InvocationContextOutcomeCodes.DisallowedFact,
                    null);
            }

            if (fact.Ownership is not null && fact.Ownership != session.Ownership)
            {
                return new InvocationContextAssembleResult(
                    false,
                    InvocationContextOutcomeCodes.OwnershipMismatch,
                    null);
            }

            if (fact.Category == InvocationContextFactCategories.SubmissionRef
                && !session.Binding.PermittedSubmissionRefs.Any(reference =>
                    string.Equals(reference.ProtectedRef, fact.Value, StringComparison.Ordinal)))
            {
                return new InvocationContextAssembleResult(
                    false,
                    InvocationContextOutcomeCodes.UnpermittedReference,
                    null);
            }

            if (fact.Category == InvocationContextFactCategories.KnowledgeRef
                && !session.Binding.PermittedKnowledgeRefs.Any(reference =>
                    string.Equals(reference.ProtectedRef, fact.Value, StringComparison.Ordinal)))
            {
                return new InvocationContextAssembleResult(
                    false,
                    InvocationContextOutcomeCodes.UnpermittedReference,
                    null);
            }

            if (fact.Category == InvocationContextFactCategories.MemoryReadRef
                && !session.Binding.PermittedMemoryReadRefs.Any(reference =>
                    string.Equals(reference.ProtectedRef, fact.Value, StringComparison.Ordinal)))
            {
                return new InvocationContextAssembleResult(
                    false,
                    InvocationContextOutcomeCodes.UnpermittedReference,
                    null);
            }

            if (fact.Category == InvocationContextFactCategories.TranscriptItem
                && !session.VisibleTranscript.Any(item =>
                    string.Equals(item.MessageId, fact.Value, StringComparison.Ordinal)))
            {
                return new InvocationContextAssembleResult(
                    false,
                    InvocationContextOutcomeCodes.UnpermittedReference,
                    null);
            }
        }

        return new InvocationContextAssembleResult(
            true,
            InvocationContextOutcomeCodes.Succeeded,
            CreateContext(session));
    }

    private static InvocationContext CreateContext(SessionRuntime session)
    {
        var categories = new List<string>();
        if (session.Binding.PermittedSubmissionRefs.Count > 0)
        {
            categories.Add(InvocationContextFactCategories.SubmissionRef);
        }

        if (session.Binding.PermittedKnowledgeRefs.Count > 0)
        {
            categories.Add(InvocationContextFactCategories.KnowledgeRef);
        }

        if (session.Binding.PermittedMemoryReadRefs.Count > 0)
        {
            categories.Add(InvocationContextFactCategories.MemoryReadRef);
        }

        if (session.VisibleTranscript.Count > 0)
        {
            categories.Add(InvocationContextFactCategories.TranscriptItem);
        }

        return new InvocationContext(
            session.Ownership,
            session.Binding.ConfigurationDigest,
            session.Binding.Policy.PolicyDigest,
            session.Binding.PermittedSubmissionRefs,
            session.Binding.PermittedKnowledgeRefs,
            session.Binding.PermittedMemoryReadRefs,
            session.VisibleTranscript,
            categories);
    }
}
