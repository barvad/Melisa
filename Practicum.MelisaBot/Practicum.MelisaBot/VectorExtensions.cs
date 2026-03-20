namespace Practicum.MelisaBot;

public static class VectorExtensions
{
    public static double Similarity(this float[] vectorA, float[] vectorB)
    {
        if (vectorA.Length != vectorB.Length)
            throw new ArgumentException("Массивы должны быть одинаковой длины");

        double dotProduct = 0;
        double normA = 0;
        double normB = 0;

        for (int i = 0; i < vectorA.Length; i++)
        {
            dotProduct += vectorA[i] * vectorB[i];
            normA += Math.Pow(vectorA[i], 2);
            normB += Math.Pow(vectorB[i], 2);
        }

        double denominator = Math.Sqrt(normA) * Math.Sqrt(normB);
        return denominator == 0 ? 0 : dotProduct / denominator;
    }
}