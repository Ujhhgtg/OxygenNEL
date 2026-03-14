using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OxygenNEL.Core.Utils;

public class TextComponent
{
    [JsonPropertyName("text")] public string Text { get; set; } = string.Empty;
    [JsonPropertyName("translate")] public string Translate { get; set; } = string.Empty;
    [JsonPropertyName("color")] public string Color { get; set; } = string.Empty;
    [JsonPropertyName("bold")] public bool Bold { get; set; }
    [JsonPropertyName("extra")] public List<TextComponent> Extra { get; set; } = [];
    [JsonIgnore] public string FullText { get; set; } = string.Empty;

    [JsonIgnore]
    public string DisplayText
    {
        get
        {
            if (!string.IsNullOrEmpty(FullText)) return FullText;
            return string.IsNullOrEmpty(Text) ? Translate : Text;
        }
    }

    public string ToJson()
    {
        return JsonSerializer.Serialize(this);
    }
}