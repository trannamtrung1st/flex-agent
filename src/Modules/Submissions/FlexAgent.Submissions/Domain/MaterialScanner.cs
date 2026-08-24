namespace FlexAgent.Submissions.Domain;

public enum MaterialScannerMode
{
    DisabledByApprovedPolicy,
    Required,
}

public enum MaterialScanOutcome
{
    Clean,
    Rejected,
    Inconclusive,
    Unavailable,
}
