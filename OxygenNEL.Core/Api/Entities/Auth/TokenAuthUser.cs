using System.Text.Json.Serialization;

namespace OxygenNEL.Core.Api.Entities.Auth;

public class TokenAuthUser
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("username")] public string Username { get; set; } = string.Empty;
    [JsonPropertyName("email")] public string Email { get; set; } = string.Empty;
    [JsonPropertyName("rank")] public string Rank { get; set; }
    [JsonPropertyName("is_admin")] public bool IsAdmin { get; set; }
}