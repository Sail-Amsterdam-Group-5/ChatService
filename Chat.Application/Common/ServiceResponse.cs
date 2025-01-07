namespace Chat.Application.Common;

public class ServiceResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();

    public static ServiceResponse<T> SuccessResponse(T data, string message = "Operation completed successfully")
    {
        return new ServiceResponse<T>
        {
            Success = true,
            Message = message,
            Data = data
        };
    }

    public static ServiceResponse<T> ErrorResponse(string message, List<string>? errors = null)
    {
        return new ServiceResponse<T>
        {
            Success = false,
            Message = message,
            Errors = errors ?? new List<string>()
        };
    }
}