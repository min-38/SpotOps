namespace SpotOps.Contracts;

public sealed record ApiError(
    string Code,
    string? Message = null,
    IReadOnlyDictionary<string, string[]>? Details = null);

// JSON API 공통 래퍼
public sealed class ApiResponse<T>
{
    public T? Data { get; }
    public ApiError? Error { get; }
    public bool Success => Error is null;

    private ApiResponse(T? data, ApiError? error)
    {
        Data = data;
        Error = error;
    }

    public static ApiResponse<T> Ok(T? data = default) => new(data, null);

    public static ApiResponse<T> Fail(string code, string? message = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Error code is required.", nameof(code));

        var trimmed = code.Trim();
        return new(default, new ApiError(trimmed, string.IsNullOrWhiteSpace(message) ? null : message.Trim()));
    }

    public static ApiResponse<T> Fail(
        string code,
        string? message,
        IReadOnlyDictionary<string, string[]> details)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Error code is required.", nameof(code));

        var trimmed = code.Trim();
        return new(default, new ApiError(trimmed, string.IsNullOrWhiteSpace(message) ? null : message.Trim(), details));
    }
}
