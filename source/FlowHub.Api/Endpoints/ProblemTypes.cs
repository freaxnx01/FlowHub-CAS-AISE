namespace FlowHub.Api.Endpoints;

internal static class ProblemTypes
{
    private const string Base = "https://github.com/freaxnx01/FlowHub-CAS-AISE/blob/main/docs/problems/";
    public const string Validation = Base + "validation.md";
    public const string CaptureNotFound = Base + "capture-not-found.md";
    public const string CaptureNotRetryable = Base + "capture-not-retryable.md";
}
