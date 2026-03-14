using System.Text.Json.Serialization;

namespace OxygenNEL.Core.Api.Entities.Auth;

public class TokenAuthResponse : ApiResponse
{
    [JsonPropertyName("user")] public TokenAuthUser User { get; set; }
}