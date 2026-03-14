using System.Text.Json.Serialization;

namespace OxygenNEL.Core.Api.Entities.Auth;

public class UserInfoResponse : ApiResponse
{
    [JsonPropertyName("id")] public long? Id { get; set; }
    [JsonPropertyName("username")] public string Username { get; set; }
    [JsonPropertyName("email")] public string Email { get; set; }
    [JsonPropertyName("avatar")] public string Avatar { get; set; }
    [JsonPropertyName("rank")] public string Rank { get; set; }
    [JsonPropertyName("banned")] public int Banned { get; set; }
    [JsonPropertyName("isAdmin")] public int IsAdmin { get; set; }
    [JsonPropertyName("createdAt")] public string CreatedAt { get; set; }
    [JsonPropertyName("lastLogin")] public string LastLogin { get; set; }
}