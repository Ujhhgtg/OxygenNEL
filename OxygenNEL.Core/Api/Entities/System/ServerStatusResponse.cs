using System.Text.Json.Serialization;

namespace OxygenNEL.Core.Api.Entities.System;

public class ServerStatusResponse : ApiResponse
{
    [JsonPropertyName("uptime")] public long Uptime { get; set; }
    [JsonPropertyName("totalUsers")] public int TotalUsers { get; set; }
    [JsonPropertyName("cpu")] public string Cpu { get; set; }
    [JsonPropertyName("memory")] public MemoryInfo Memory { get; set; }
}