using Microsoft.ML.Data;

namespace Practicum.MelisaBot;

public class ModelOutput
{
    [ColumnName("last_hidden_state")] public float[] LastHiddenState { get; set; }
}