using System.Net;
using Microsoft.EntityFrameworkCore;
using Practicum.MelisaBot.Ef;
using Practicum.MelisaBot.Groq;
using Practicum.MelisaBot.Infrastructure;
using Practicum.MelisaBot.Telegram;
using Refit;
using Telegram.Bot;

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
        var cl = new WebClient();
        Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models"));
        var modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "model.onnx");
        var vocabPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "vocab.txt");
        var pytorchModel = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "pytorch_model.bin");
        if (!File.Exists(modelPath))
            cl.DownloadFile(
                "https://huggingface.co/Xenova/distiluse-base-multilingual-cased-v2/resolve/main/onnx/model.onnx",
                modelPath);
        if (!File.Exists(vocabPath))
            cl.DownloadFile("https://huggingface.co/Xenova/distiluse-base-multilingual-cased-v2/resolve/main/vocab.txt",
                vocabPath);
        if (!File.Exists(pytorchModel))
            cl.DownloadFile(
                "https://huggingface.co/sentence-transformers/distiluse-base-multilingual-cased-v2/resolve/main/2_Dense/pytorch_model.bin",
                pytorchModel);
        builder.Services.AddHttpClient("tgclient")
            .AddTypedClient<ITelegramBotClient>(httpClient =>
                new TelegramBotClient(builder.Configuration["TgBotApiKey"]!, httpClient));
        
        // Фоновая служба бота
        builder.Services.AddHostedService<TelegramBotBackgroundService>();
        builder.Services.AddSingleton<ITextChunkerWithOverlap>(new TextChunkerWithOverlap(vocabPath));
        var generator = new EmbeddingGenerator(modelPath, vocabPath, new DenseWeights(pytorchModel));
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

        app.UseSwagger();
        app.UseSwaggerUI();


        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}