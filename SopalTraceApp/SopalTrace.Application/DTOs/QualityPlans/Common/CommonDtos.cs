using System.Collections.Generic;

namespace SopalTrace.Application.DTOs.QualityPlans.Common;


public record ApiResponseDto<T>(
    bool Success,
    string Message,
    T Data,
    DateTime Timestamp = default
)
{
    public ApiResponseDto(bool success, T data, string message)
        : this(success, message, data, DateTime.UtcNow)
    {
    }
}


public record PaginatedResponseDto<T>(
    List<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize
)
{
    public int TotalPages => (TotalCount + PageSize - 1) / PageSize;

    public PaginatedResponseDto() : this(new(), 0, 1, 10)
    {
    }
}


public record ApiErrorDto(
    string Code,
    string Message,
    Dictionary<string, string> Details,
    DateTime Timestamp = default
)
{
    public ApiErrorDto(string code, string message, Dictionary<string, string>? details = null)
        : this(code, message, details ?? new(), DateTime.UtcNow)
    {
    }
}


public record FilterParamsDto(
    string SearchTerm,
    int Page,
    int PageSize
)
{
    public FilterParamsDto() : this(string.Empty, 1, 10)
    {
    }

    public int ValidatedPageSize => PageSize > 0 && PageSize <= 100 ? PageSize : 10;
    public int ValidatedPage => Page > 0 ? Page : 1;
}
