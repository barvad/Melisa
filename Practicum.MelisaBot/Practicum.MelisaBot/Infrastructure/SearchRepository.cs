using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using Practicum.MelisaBot.Ef;

namespace Practicum.MelisaBot.Infrastructure;

public class SearchRepository : ISearchRepository
{
    private readonly IDbContextFactory<MelisaDbContext> _dbFactory;
    private readonly IEmbeddingGenerator _embeddingGenerator;
    private readonly ILogger<SearchRepository> _logger;


    public SearchRepository(IDbContextFactory<MelisaDbContext> dbFactory, IEmbeddingGenerator embeddingGenerator,ILogger<SearchRepository> logger)
    {
        _dbFactory = dbFactory;
        _embeddingGenerator = embeddingGenerator;
        _logger = logger;
    }


    public async Task AddChunkAsync(string chunkText, string url)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var vector = _embeddingGenerator.GenerateEmbedding(chunkText, false);
        var chunk = new Chunk
        {
            Url = url,
            Text = chunkText,
            Embedding = new Vector(new ReadOnlyMemory<float>(vector)) // Преобразуем float[] в Vector
        };

        db.Chunks.Add(chunk);
        await db.SaveChangesAsync();    
    }

    public async Task<string> SearchAsync(string queryText, int limit = 5)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var sw = new Stopwatch();
        sw.Start();
        var vector = new Vector(new ReadOnlyMemory<float>(_embeddingGenerator.GenerateEmbedding(queryText)));

        var result = db.Chunks
            .AsNoTracking()
            .Select(p => new
            {
                p.Id,
                p.Url,
                p.Text ,
                Score = 1 - p.Embedding!.CosineDistance(vector)
            })
           // .Where(p => p.Score > 0.3)
            .OrderByDescending(p => p.Score)
            .Take(limit);

        var chunksList = await result.ToListAsync();
        sw.Stop();
        _logger.LogInformation($"Query {queryText} search time:{sw.ElapsedMilliseconds}ms");
       
        var json = JsonSerializer.Serialize(chunksList);
        return json;
    }
}