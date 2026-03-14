using System.Text.Json.Serialization;

namespace OxygenNEL.Core.Api.Entities.Auth;

public class DurationResponse : ApiResponse
{
    [JsonPropertyName("duration")]
    public string? Duration { get; set; }
}
