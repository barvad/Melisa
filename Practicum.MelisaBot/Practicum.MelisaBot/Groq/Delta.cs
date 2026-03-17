using System.Text.Json.Serialization;

namespace Practicum.MelisaBot.Groq;

public class Delta
{
    [JsonPropertyName("content")]
    public string Content { get; set; }
}