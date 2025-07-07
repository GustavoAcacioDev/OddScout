using OddScout.Application.Common.Models;

namespace OddScout.Application.DTOs;

public class EnhancedPagedResult<T> : IApiResponse
{
    // Propriedades existentes do PagedResult (mantendo compatibilidade)
    public List<T> Items { get; set; } = new();
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage { get; set; }
    public bool HasNextPage { get; set; }

    // Novas propriedades do padrão ApiResponse
    public bool IsSuccess { get; set; } = true;
    public bool HasWarnings { get; set; } = false;
    public string[] Errors { get; set; } = Array.Empty<string>();
    public string[] Warnings { get; set; } = Array.Empty<string>();

    public EnhancedPagedResult() { }

    public EnhancedPagedResult(List<T> items, int pageNumber, int pageSize, int totalCount)
    {
        Items = items;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalCount = totalCount;
        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        HasPreviousPage = pageNumber > 1;
        HasNextPage = pageNumber < TotalPages;
    }

    // Método para converter PagedResult existente para EnhancedPagedResult
    public static EnhancedPagedResult<T> FromPagedResult(PagedResult<T> pagedResult, string[]? warnings = null)
    {
        return new EnhancedPagedResult<T>
        {
            Items = pagedResult.Items,
            PageNumber = pagedResult.PageNumber,
            PageSize = pagedResult.PageSize,
            TotalCount = pagedResult.TotalCount,
            TotalPages = pagedResult.TotalPages,
            HasPreviousPage = pagedResult.HasPreviousPage,
            HasNextPage = pagedResult.HasNextPage,
            IsSuccess = true,
            HasWarnings = warnings?.Length > 0,
            Warnings = warnings ?? Array.Empty<string>()
        };
    }

    public static EnhancedPagedResult<T> Success(List<T> items, int pageNumber, int pageSize, int totalCount, string[]? warnings = null)
    {
        var result = new EnhancedPagedResult<T>(items, pageNumber, pageSize, totalCount);
        result.HasWarnings = warnings?.Length > 0;
        result.Warnings = warnings ?? Array.Empty<string>();
        return result;
    }

    public static EnhancedPagedResult<T> Failure(string[] errors)
    {
        return new EnhancedPagedResult<T>
        {
            Items = new List<T>(),
            PageNumber = 0,
            PageSize = 0,
            TotalCount = 0,
            TotalPages = 0,
            HasPreviousPage = false,
            HasNextPage = false,
            IsSuccess = false,
            HasWarnings = false,
            Errors = errors
        };
    }

    // Conversão implícita para ApiResponseList (para uso no middleware)
    public ApiResponseList<T> ToApiResponseList()
    {
        return new ApiResponseList<T>
        {
            Items = Items,
            Total = TotalCount,
            PageIndex = PageNumber,
            IsSuccess = IsSuccess,
            HasWarnings = HasWarnings,
            Errors = Errors,
            Warnings = Warnings
        };
    }
}