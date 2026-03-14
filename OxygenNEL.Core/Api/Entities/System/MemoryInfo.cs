using System.Text.Json.Serialization;

namespace OxygenNEL.Core.Api.Entities.System;

public class MemoryInfo
{
    [JsonPropertyName("total")] public long Total { get; set; }
    [JsonPropertyName("free")] public long Free { get; set; }
    [JsonPropertyName("used")] public long Used { get; set; }
}