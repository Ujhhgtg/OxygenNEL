using System.Text.Json.Serialization;

namespace OxygenNEL.Core.Api.Entities.Auth;

public class CardKeyActivateResponse : ApiResponse
{
    [JsonPropertyName("duration")]
    public string? Duration { get; set; }
}
