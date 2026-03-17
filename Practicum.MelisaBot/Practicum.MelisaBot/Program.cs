using Microsoft.EntityFrameworkCore;
using Pgvector;
using Practicum.MelisaBot.Ef;
using Practicum.MelisaBot.Infrastructure;
using System.Reflection.Emit;
using Practicum.MelisaBot.Groq;
using Refit;

namespace Practicum.MelisaBot;

internal class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);


        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
        builder.Services.AddHostedService<QueuedHostedService>();
        builder.Services.AddDbContext();
        builder.Services.AddScoped<ISearchRepository, SearchRepository>();

        var modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "model.onnx");
        var vocabPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "vocab.txt");
        builder.Services.AddSingleton<ITextChunkerWithOverlap>(new TextChunkerWithOverlap(vocabPath));
        var generator = new EmbeddingGenerator(modelPath, vocabPath);
        builder.Services.AddSingleton<IEmbeddingGenerator>(generator);
        var apiKey = builder.Configuration["GroqApiKey"];

        builder.Services.AddRefitClient<IGroqApi>()
            .ConfigureHttpClient(c =>
            {
                c.BaseAddress = new Uri("https://api.groq.com");
                c.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            });
        builder.Services.AddScoped<IGroqClient, GroqClient>();
        var app = builder.Build();
        using (var scope = app.Services.CreateScope())
        {
            var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<MelisaDbContext>>();
            using var dbContext = dbContextFactory.CreateDbContext();
            dbContext.Database.Migrate();

        }

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}