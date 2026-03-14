using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OxygenNEL.Core.Api.Entities.System;

public class PluginListResponse : ApiResponse
{
    [JsonPropertyName("plugins")] public List<PluginInfo> Plugins { get; set; }
}