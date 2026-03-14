using System.Text.Json.Serialization;

namespace OxygenNEL.Core.Api.Entities.System;

public class PluginInfo
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("version")] public string Version { get; set; } = string.Empty;
    [JsonPropertyName("logoUrl")] public string LogoUrl { get; set; }
    [JsonPropertyName("shortDescription")] public string ShortDescription { get; set; }
    [JsonPropertyName("publisher")] public string Publisher { get; set; }
    [JsonPropertyName("downloadUrl")] public string DownloadUrl { get; set; }
    [JsonPropertyName("depends")] public string Depends { get; set; }
}