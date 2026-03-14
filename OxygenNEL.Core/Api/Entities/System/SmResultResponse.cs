using System.Text.Json.Serialization;

namespace OxygenNEL.Core.Api.Entities.System;

public class SmResultResponse : ApiResponse
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("details")]
    public object? Details { get; set; }
}
