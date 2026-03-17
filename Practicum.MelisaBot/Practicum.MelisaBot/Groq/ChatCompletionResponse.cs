using System.Text.Json.Serialization;

namespace Practicum.MelisaBot.Groq;

public class ChatCompletionResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("choices")]
    public List<Choice> Choices { get; set; }
}