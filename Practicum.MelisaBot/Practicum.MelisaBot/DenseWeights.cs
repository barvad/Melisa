using System.IO.Compression;
using System.Numerics.Tensors;

namespace Practicum.MelisaBot;

public class DenseWeights
{
    private readonly float[] _weights; // 512 строк по 768 колонок
    private readonly float[] _bias;    // 512 значений

    public DenseWeights(string binPath)
    {
        using var archive = ZipFile.OpenRead(binPath);

        // В PyTorch файлах веса обычно лежат в 'archive/data/0' (матрица) и '1' (bias)
        // Но порядок может зависеть от версии. Проверим размеры.
        _weights = ExtractFloatArray(archive, "archive/data/0", 768 * 512);
        _bias = ExtractFloatArray(archive, "archive/data/1", 512);
    }

    private float[] ExtractFloatArray(ZipArchive archive, string entryName, int expectedCount)
    {
        var entry = archive.GetEntry(entryName)
                    ?? throw new Exception($"Не найден файл {entryName} внутри .bin архива");

        using var stream = entry.Open();
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        byte[] bytes = ms.ToArray();

        // Проверяем, что данных достаточно (4 байта на каждый float)
        if (bytes.Length < expectedCount * 4)
            throw new Exception($"Недостаточно данных в {entryName}. Ожидалось {expectedCount * 4} байт.");

        // Превращаем байты в float[]
        float[] result = new float[expectedCount];
        Buffer.BlockCopy(bytes, 0, result, 0, expectedCount * 4);
        return result;
    }

    public float[] ProjectAndNormalize(float[] pooled768)
    {
        float[] output = new float[512];

        for (int i = 0; i < 512; i++)
        {
            // Берем "строку" матрицы весов для текущего i
            var weightRow = _weights.AsSpan(i * 768, 768);

            // SIMD-ускоренное перемножение (Dot Product) + прибавляем смещение
            output[i] = TensorPrimitives.Dot(pooled768, weightRow) + _bias[i];
        }

        // L2-нормализация для косинусного сходства в pgvector
        float norm = TensorPrimitives.Norm(output);
        TensorPrimitives.Divide(output, norm, output);

        return output;
    }
}