namespace OddScout.Application.Common.Models;

public interface IApiResponse
{
    bool IsSuccess { get; set; }
    bool HasWarnings { get; set; }
    string[] Errors { get; set; }
    string[] Warnings { get; set; }
}

public class ApiResponse<T> : IApiResponse
{
    public T Value { get; set; } = default!;
    public bool IsSuccess { get; set; } = true;
    public bool HasWarnings { get; set; } = false;
    public string[] Errors { get; set; } = Array.Empty<string>();
    public string[] Warnings { get; set; } = Array.Empty<string>();

    public ApiResponse() { }

    public ApiResponse(T value)
    {
        Value = value;
    }

    public static ApiResponse<T> Success(T value, string[]? warnings = null)
    {
        return new ApiResponse<T>
        {
            Value = value,
            IsSuccess = true,
            HasWarnings = warnings?.Length > 0,
            Warnings = warnings ?? Array.Empty<string>()
        };
    }

    public static ApiResponse<T> Failure(string[] errors, T? value = default)
    {
        return new ApiResponse<T>
        {
            Value = value!,
            IsSuccess = false,
            HasWarnings = false,
            Errors = errors
        };
    }
}

public class ApiResponseList<T> : IApiResponse
{
    public int Total { get; set; }
    public int PageIndex { get; set; }
    public List<T> Items { get; set; } = new();
    public bool IsSuccess { get; set; } = true;
    public bool HasWarnings { get; set; } = false;
    public string[] Errors { get; set; } = Array.Empty<string>();
    public string[] Warnings { get; set; } = Array.Empty<string>();

    public ApiResponseList() { }

    public ApiResponseList(List<T> items, int total = 0, int pageIndex = 0)
    {
        Items = items;
        Total = total > 0 ? total : items.Count;
        PageIndex = pageIndex;
    }

    public static ApiResponseList<T> Success(List<T> items, int total = 0, int pageIndex = 0, string[]? warnings = null)
    {
        return new ApiResponseList<T>
        {
            Items = items,
            Total = total > 0 ? total : items.Count,
            PageIndex = pageIndex,
            IsSuccess = true,
            HasWarnings = warnings?.Length > 0,
            Warnings = warnings ?? Array.Empty<string>()
        };
    }

    public static ApiResponseList<T> Failure(string[] errors, List<T>? items = null)
    {
        return new ApiResponseList<T>
        {
            Items = items ?? new List<T>(),
            Total = 0,
            PageIndex = 0,
            IsSuccess = false,
            HasWarnings = false,
            Errors = errors
        };
    }
}