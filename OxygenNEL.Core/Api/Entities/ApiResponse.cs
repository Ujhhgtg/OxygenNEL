using System.Text.Json.Serialization;

namespace OxygenNEL.Core.Api.Entities;

public class ApiResponse
{
    [JsonPropertyName("success")] public bool Success { get; set; }
    [JsonPropertyName("message")] public string Message { get; set; }

    public static ApiResponse Fail(string message)
    {
        return new ApiResponse
        {
            Success = false,
            Message = message
        };
    }

    public static ApiResponse Ok(string message = null)
    {
        return new ApiResponse
        {
            Success = true,
            Message = message
        };
    }
}