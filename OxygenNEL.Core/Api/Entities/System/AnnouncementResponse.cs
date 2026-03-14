using System.Text.Json.Serialization;

namespace OxygenNEL.Core.Api.Entities.System;

public class AnnouncementResponse : ApiResponse
{
    [JsonPropertyName("title")] public string Title { get; set; }
    [JsonPropertyName("content")] public string Content { get; set; }
    [JsonPropertyName("level")] public string Level { get; set; }
    [JsonPropertyName("updated")] public string Updated { get; set; }
}