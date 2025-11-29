namespace SharedKernel;

public sealed class ApiResponse<T>
{

    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }

    public static ApiResponse<T> Success(T data, string? message = null)
    {
        return new ApiResponse<T>
        {
            IsSuccess = true,
            Message = message ?? "Operation completed successfully.",
            Data = data
        };
    }

    public static ApiResponse<T> NotFound(string? message = null)
    {
        return new ApiResponse<T>
        {
            IsSuccess = false,
            Message = message ?? "The requested item was not found.",
            Data = default
        };
    }

    public static ApiResponse<T> Error(string message)
    {
        return new ApiResponse<T>
        {
            IsSuccess = false,
            Message = message,
            Data = default
        };
    }

    public static ApiResponse<T> ValidationError(string? message = null)
    {
        return new ApiResponse<T>
        {
            IsSuccess = false,
            Message = message ?? "Validation failed.",
            Data = default
        };
    }
}
