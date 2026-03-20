using System.Text.Json.Serialization;

namespace Practicum.MelisaBot.Groq;

public class Choice
{
    [JsonPropertyName("delta")]
    public Delta Delta { get; set; }

    [JsonPropertyName("message")]
    public Message Message { get; set; }

    [JsonPropertyName("finish_reason")]
    public string FinishReason { get; set; }
}