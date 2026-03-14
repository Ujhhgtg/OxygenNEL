using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OxygenNEL.Core.Api.Entities.System;

public class PluginListWithItems : ApiResponse
{
    [JsonPropertyName("items")] public List<PluginInfo> Items { get; set; }
}