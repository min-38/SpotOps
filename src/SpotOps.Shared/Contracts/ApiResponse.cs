namespace SpotOps.Contracts;

public sealed record ApiError(
    string Code,
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

    public static ApiResponse<T> Fail(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Error code is required.", nameof(code));

        var trimmed = code.Trim();
        return new(default, new ApiError(trimmed));
    }

    public static ApiResponse<T> Fail(
        string code,
        IReadOnlyDictionary<string, string[]> details)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Error code is required.", nameof(code));

        var trimmed = code.Trim();
        return new(default, new ApiError(trimmed, details));
    }
}
