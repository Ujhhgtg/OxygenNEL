using System.Text.Json.Serialization;

namespace OxygenNEL.Core.Api.Entities.Auth;

public class LoginData
{
    [JsonPropertyName("token")] public string Token { get; set; } = string.Empty;
}