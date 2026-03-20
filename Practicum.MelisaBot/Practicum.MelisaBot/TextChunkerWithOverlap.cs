using Microsoft.ML.Tokenizers;

namespace Practicum.MelisaBot;

public class TextChunkerWithOverlap : ITextChunkerWithOverlap
{
    private readonly BertTokenizer _tokenizer;
    private readonly int _maxTokens;
    private readonly int _overlapTokens;

    public TextChunkerWithOverlap(string vocabPath, int maxTokens = 450, int overlapTokens = 50)
    {
        _tokenizer = BertTokenizer.Create(vocabPath);
        _maxTokens = maxTokens;
        _overlapTokens = overlapTokens;
    }

    public List<string> CreateChunks(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return new List<string>();

        //  Разбиваем на предложения
        var sentences = text.Split(new[] { ". ", "! ", "? " }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim() + ".")
            .ToList();

        // Кэш для хранения количества токенов каждого предложения (индекс -> кол-во токенов)
        var sentenceTokenCounts = new int[sentences.Count];
        for (int i = 0; i < sentences.Count; i++)
        {
            sentenceTokenCounts[i] = _tokenizer.EncodeToTokens(sentences[i], out _).Count;
        }

        var chunks = new List<string>();
        var currentChunkSentences = new List<string>();
        var currentChunkTokenIndices = new List<int>(); // Храним индексы предложений в текущем чанке
        int currentTokenCount = 0;

        for (int i = 0; i < sentences.Count; i++)
        {
            int sentenceTokens = sentenceTokenCounts[i];

            // Защита: если предложение само по себе больше лимита, ограничиваем его виртуально
            if (sentenceTokens > _maxTokens) sentenceTokens = _maxTokens;

            // Если добавление предложения превысит лимит
            if (currentTokenCount + sentenceTokens > _maxTokens && currentChunkSentences.Any())
            {
                // Сохраняем текущий чанк
                chunks.Add(string.Join(" ", currentChunkSentences));

                // Формируем перекрытие (Overlap)
                var overlapSentences = new List<string>();
                var overlapIndices = new List<int>();
                int overlapCount = 0;

                // Идем назад по текущему чанку
                for (int j = currentChunkSentences.Count - 1; j >= 0; j--)
                {
                    int sTokens = sentenceTokenCounts[currentChunkTokenIndices[j]];

                    // Добавляем в оверлап, пока не превысим _overlapTokens
                    if (overlapCount + sTokens <= _overlapTokens)
                    {
                        overlapSentences.Insert(0, currentChunkSentences[j]);
                        overlapIndices.Insert(0, currentChunkTokenIndices[j]);
                        overlapCount += sTokens;
                    }
                    else break;
                }

                // Проверка: если оверлап съел весь лимит (очень длинное предложение), 
                // сбрасываем его, чтобы не зациклиться
                if (overlapCount >= _maxTokens - 10)
                {
                    overlapSentences.Clear();
                    overlapIndices.Clear();
                    overlapCount = 0;
                }

                currentChunkSentences = overlapSentences;
                currentChunkTokenIndices = overlapIndices;
                currentTokenCount = overlapCount;
            }

            currentChunkSentences.Add(sentences[i]);
            currentChunkTokenIndices.Add(i);
            currentTokenCount += sentenceTokens;
        }

        // Добавляем последний накопившийся чанк
        if (currentChunkSentences.Any())
        {
            chunks.Add(string.Join(" ", currentChunkSentences));
        }

        return chunks;
    }

}