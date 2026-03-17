namespace Practicum.MelisaBot.Groq;

public interface IGroqClient
{
    Task<string> SendMessageAsync(string content);
}