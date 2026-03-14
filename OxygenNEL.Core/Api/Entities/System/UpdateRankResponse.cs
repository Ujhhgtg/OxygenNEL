using System.Text.Json.Serialization;

namespace OxygenNEL.Core.Api.Entities.System;

public class UpdateRankResponse : ApiResponse
{
    [JsonPropertyName("username")] public string Username { get; set; }
    [JsonPropertyName("rank")] public string Rank { get; set; }
}