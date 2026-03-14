using System.Text.Json.Serialization;

namespace OxygenNEL.Core.Api.Entities.Auth;

public class CrcSaltResponse : ApiResponse
{
    [JsonPropertyName("salt")] public string Salt { get; set; }
    [JsonPropertyName("gameVersion")] public string GameVersion { get; set; }
    [JsonPropertyName("id")] public long? Id { get; set; }
}