using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Tokenizers;
using Microsoft.ML.Transforms.Onnx;

namespace Practicum.MelisaBot;



public class EmbeddingGenerator : IEmbeddingGenerator
{
    private readonly DenseWeights _denseWeights;
    private readonly MLContext _mlContext;
    Tokenizer _tokenizer;
    private readonly ITransformer _transformer;

    public EmbeddingGenerator(string modelPath, string vocabPath, DenseWeights denseWeights)
    {
        _denseWeights = denseWeights;

        _mlContext = new MLContext();
        _tokenizer = BertTokenizer.Create(vocabPath);

        var inputShapes = new Dictionary<string, int[]>
        {
            { "input_ids", new[] { 1, 256 } },
            { "attention_mask", new[] { 1, 256 } },
            { "token_type_ids", new[] { 1, 256 } }
        };

        // Описываем Pipeline
        var pipeline = _mlContext.Transforms.ApplyOnnxModel(
            modelFile: modelPath,
            outputColumnNames: new[] { "last_hidden_state" },
            inputColumnNames: new[] { "input_ids", "attention_mask"/*, "token_type_ids"*/ },
            shapeDictionary: inputShapes,
            gpuDeviceId: null,
            fallbackToCpu: true
        );

        // Инициализируем движок предсказаний
        var emptyData = _mlContext.Data.LoadFromEnumerable(new List<ModelInput>());
        var model = pipeline.Fit(emptyData);
        _transformer = pipeline.Fit(emptyData);
    }


    public float[] GenerateEmbedding(string text, bool isQuery = true)
    {
        string formattedText = /*(isQuery ? "query: " : "passage: ") +*/ text;
        var tokens = _tokenizer.EncodeToTokens(formattedText, out _);

        // Создаем строго фиксированные массивы
        var input = new ModelInput
        {
            InputIds = new long[256],
            AttentionMask = new long[256],
            TokenTypeIds = new long[256]
        };

       
        input.InputIds[0] = 101;
        input.AttentionMask[0] = 1;
        int count = Math.Min(tokens.Count, 254);
        for (int i = 0; i < count; i++)
        {
            input.InputIds[i + 1] = (long)tokens[i].Id;
            input.AttentionMask[i + 1] = 1;
        }
        input.InputIds[count + 1] = 102;
        input.AttentionMask[count + 1] = 1;

        var dataView = _mlContext.Data.LoadFromEnumerable(new[] { input });
        var transformedData = _transformer.Transform(dataView);

        // Извлекаем результат
        var outputColumn = transformedData.GetColumn<float[]>("last_hidden_state").FirstOrDefault();

        if (outputColumn == null) throw new Exception("Модель не вернула данные");

        var poolingVector = MeanPooling(outputColumn, input.AttentionMask);
       return _denseWeights.ProjectAndNormalize(poolingVector);
    }

   
    private float[] MeanPooling(float[] lastHiddenState, long[] attentionMask)
    {
        int dim = 768;
        float[] mean = new float[dim];
        int count = 0;

        for (int i = 0; i < attentionMask.Length; i++)
        {
            if (attentionMask[i] == 0) continue;
            count++;
            for (int d = 0; d < dim; d++)
                mean[d] += lastHiddenState[i * dim + d];
        }

        for (int d = 0; d < dim; d++) mean[d] /= count;
        return mean;
    }

   
}