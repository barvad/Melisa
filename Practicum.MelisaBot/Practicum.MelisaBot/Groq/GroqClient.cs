using Refit;

namespace Practicum.MelisaBot.Groq
{
    public class GroqClient : IGroqClient
    {
        private readonly IGroqApi _groqApi;

        public GroqClient(IGroqApi groqApi)
        {
            _groqApi = groqApi;
        }

        public async Task<string> SendMessageAsync(string content)
        {
            var request = new ChatCompletionRequest
            {
                Messages = new List<ChatMessage>
                {
                    new ChatMessage
                    {
                        Role = "user",
                        Content = content
                    }
                },
                Model = "openai/gpt-oss-120b",
                Temperature = 1,
                MaxCompletionTokens = 8192,
                TopP = 1,
                Stream = false,
                ReasoningEffort = "medium",
                Stop = null
            };

            try
            {
                var response = await _groqApi.CreateChatCompletion(
                    request);

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    // Обработка ответа
                    var result = response.Content;
                    return result.Choices[0].Message.Content ?? "No content";
                }
                else
                {
                    var error = await response.Error.GetContentAsAsync<dynamic>();
                    throw new Exception($"API Error: {error}");
                }
            }
            catch (ApiException ex)
            {
                throw new Exception($"API Exception: {ex.Message}", ex);
            }
        }


    }
}