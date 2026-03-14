using System.Text.Json.Serialization;

namespace OxygenNEL.Core.Api.Entities.System;

public class VersionResponse : ApiResponse
{
    [JsonPropertyName("version")] public string Version { get; set; }
    [JsonPropertyName("time")] public string Time { get; set; }
    [JsonPropertyName("downloadUrl")] public string DownloadUrl { get; set; }
}