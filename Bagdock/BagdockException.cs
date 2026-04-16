namespace Bagdock;

public class BagdockException : Exception
{
    public BagdockException(string message) : base(message) { }
    public BagdockException(string message, Exception inner) : base(message, inner) { }
}

public class BagdockApiException : BagdockException
{
    public int StatusCode { get; }
    public string ErrorCode { get; }
    public string? RequestId { get; }

    public BagdockApiException(int statusCode, string errorCode, string message, string? requestId = null)
        : base($"API error {statusCode}: {errorCode} — {message}")
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
        RequestId = requestId;
    }
}
