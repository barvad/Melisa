using Microsoft.ML.Data;

namespace Practicum.MelisaBot;

public class ModelInput
{
    [VectorType(1, 256)][ColumnName("input_ids")] public long[] InputIds { get; set; } 
    [VectorType(1, 256)][ColumnName("attention_mask")] public long[] AttentionMask { get; set; }
    [VectorType(1, 256)][ColumnName("token_type_ids")] public long[] TokenTypeIds { get; set; }
}