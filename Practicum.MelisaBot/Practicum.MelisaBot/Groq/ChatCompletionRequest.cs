using System.Text.Json.Serialization;

namespace Practicum.MelisaBot.Groq;

public class ChatCompletionRequest
{
    [JsonPropertyName("messages")]
    public List<ChatMessage> Messages { get; set; }

    [JsonPropertyName("model")]
    public string Model { get; set; } = "openai/gpt-oss-120b";

    [JsonPropertyName("temperature")]
    public double Temperature { get; set; } = 1;

    [JsonPropertyName("max_completion_tokens")]
    public int MaxCompletionTokens { get; set; } = 8192;

    [JsonPropertyName("top_p")]
    public double TopP { get; set; } = 1;

    [JsonPropertyName("stream")]
    public bool Stream { get; set; } = true;

    [JsonPropertyName("reasoning_effort")]
    public string ReasoningEffort { get; set; } = "medium";

    [JsonPropertyName("stop")]
    public object Stop { get; set; } = null;
}