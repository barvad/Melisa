namespace Practicum.MelisaBot;

public interface ITextChunkerWithOverlap
{
    List<string> CreateChunks(string text);
}