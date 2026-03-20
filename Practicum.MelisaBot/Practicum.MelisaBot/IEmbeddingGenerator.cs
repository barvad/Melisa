namespace Practicum.MelisaBot;

public interface IEmbeddingGenerator
{
    float[] GenerateEmbedding(string text, bool isQuery = true);
}