using System.Text.Json.Serialization;

namespace OxygenNEL.Core.Api.Entities.Auth;

public class UserUrlResponse : ApiResponse
{
    [JsonPropertyName("userUrl")] public string UserUrl { get; set; }
}