using System.Text.Json.Serialization;

namespace OxygenNEL.Core.Api.Entities.Auth;

public class LoginResponse : ApiResponse
{
    [JsonPropertyName("token")] public string Token { get; set; }
}