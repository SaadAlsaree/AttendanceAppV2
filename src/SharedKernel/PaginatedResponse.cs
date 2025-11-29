namespace SharedKernel;

public sealed class PaginatedResponse<T>
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public IReadOnlyList<T> Data { get; set; } = [];

    public int TotalCount { get; set; }

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalPages { get; set; }

    public bool HasNextPage { get; set; }

    public bool HasPreviousPage { get; set; }

    public static PaginatedResponse<T> Create(
        IReadOnlyList<T> data,
        int totalCount,
        int page,
        int pageSize,
        string? message = null)
    {
        int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return new PaginatedResponse<T>
        {
            IsSuccess = true,
            Message = message ?? (totalCount > 0
                ? $"Successfully retrieved {data.Count} items out of {totalCount} total items."
                : "No items found matching the search criteria."),
            Data = data,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            HasNextPage = page < totalPages,
            HasPreviousPage = page > 1
        };
    }

    public static PaginatedResponse<T> Empty(int page, int pageSize, string? message = null)
    {
        return new PaginatedResponse<T>
        {
            IsSuccess = true,
            Message = message ?? "No items found matching the search criteria.",
            Data = [],
            TotalCount = 0,
            Page = page,
            PageSize = pageSize,
            TotalPages = 0,
            HasNextPage = false,
            HasPreviousPage = page > 1
        };
    }


}
