namespace OxygenNEL.Core.Api.Entities;

public class ApiResponse<T> : ApiResponse
{
    public T Data { get; set; }

    public new static ApiResponse<T> Fail(string message)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message
        };
    }

    public static ApiResponse<T> Ok(T data, string message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message
        };
    }
}