using Refit;

namespace Practicum.MelisaBot.Groq
{
    public interface IGroqApi
    {
        [Post("/openai/v1/chat/completions")]
        [Headers("Content-Type: application/json")]
        Task<ApiResponse<ChatCompletionResponse>> CreateChatCompletion(
            [Body] ChatCompletionRequest request);
    }
}