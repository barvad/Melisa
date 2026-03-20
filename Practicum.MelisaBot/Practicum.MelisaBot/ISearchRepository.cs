namespace Practicum.MelisaBot;

public interface ISearchRepository
{
    Task AddChunkAsync(string chunkText,string url);
    Task<string> SearchAsync(string queryText, int limit = 5);
}