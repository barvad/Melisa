using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection.Emit;
using Microsoft.EntityFrameworkCore.Metadata;
using Pgvector;

namespace Practicum.MelisaBot.Ef
{
    public class Chunk
    {
        public int Id { get; set; }

        [Column(TypeName = "vector(768)")]
        public Vector? Embedding { get; set; }
        public string Url { get; set; }
        public string Text { get; set; }
    }

}
