using System.Text.Json.Serialization;

namespace OxygenNEL.Core.Api.Entities.System;

public class CaptchaResponse
{
    [JsonPropertyName("result")] public string Result { get; set; }
}