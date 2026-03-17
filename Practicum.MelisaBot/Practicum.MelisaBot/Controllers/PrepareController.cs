using System.Net.Mime;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Practicum.MelisaBot.Groq;
using WikiClientLibrary.Client;
using WikiClientLibrary.Generators;
using WikiClientLibrary.Sites;

namespace Practicum.MelisaBot.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PrepareController : ControllerBase
{
    private readonly string _apiUrl;
    private readonly WikiClient _client;
    private readonly IGroqClient _groqClient;
    private readonly ILogger<PrepareController> _logger;
    private readonly ISearchRepository _searchRepository;
    private readonly WikiSite _site;
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly ITextChunkerWithOverlap _textChunker;


    private readonly string allLinksTxt = "all_links.txt";

    public PrepareController(IBackgroundTaskQueue taskQueue, ILogger<PrepareController> logger,
        ISearchRepository searchRepository, ITextChunkerWithOverlap textChunker, IGroqClient groqClient)
    {
        _taskQueue = taskQueue;
        _logger = logger;
        _searchRepository = searchRepository;
        _textChunker = textChunker;
        _groqClient = groqClient;
        _client = new WikiClient { ClientUserAgent = "Practicum.MelisaBot/1.0" };
        _apiUrl = "https://starwars.fandom.com/ru/api.php";
        _site = new WikiSite(_client, _apiUrl);
    }

    [HttpPost("start")]
    public IActionResult StartOperation()
    {
        _taskQueue.QueueBackgroundWorkItem(async token =>
        {
            //  await SaveAllLinksList();
            await PrepareDb();
        });

        return Accepted(new
        {
            message = "Операция запущена"
        });
    }

    [HttpGet("ChunksQuery")]
    public async Task<IActionResult> GetChanksText(string query, bool useTextGeneration = true)
    {
        var result = await _searchRepository.SearchAsync(query);
        if (useTextGeneration)
        {
            var resultArray = JsonNode.Parse(result).AsArray().Select(x => $"[{x?["Url"]}]\n{x?["Text"]}");
            var context = string.Join("\n", resultArray);
            var promt = System.IO.File.ReadAllText("PromtTemplate.txt").Replace("{question}", query)
                .Replace("{context}", context);
            return new JsonResult(new
            {
                answer = await _groqClient.SendMessageAsync(promt)
            });
        }

        return Content(result, MediaTypeNames.Application.Json);
    }

    private async Task SaveAllLinksList()
    {
        await _site.Initialization;

        // Получаем все страницы
        var generator = new AllPagesGenerator(_site)
        {
            NamespaceId = 0, // Основное пространство
            RedirectsFilter = PropertyFilterOption.WithoutProperty
        };

        await foreach (var page in generator.EnumPagesAsync())
            await System.IO.File.AppendAllLinesAsync(allLinksTxt,
                [$"https://starwars.fandom.com/ru/wiki/{Uri.EscapeDataString(page.Title.Replace(' ', '_'))}"]);
    }

    private async Task PrepareDb()
    {
        var links = System.IO.File.ReadLinesAsync(allLinksTxt);
        var i = 0;
        await foreach (var link in links)
        {
            i++;
            var text = await GetPageTextFromUrlAsync(link);
            if (text == null) continue;
            var chunks = _textChunker.CreateChunks(text);
            var x = 0;
            var chunksCount = chunks.Count;
            foreach (var chunk in chunks)
            {
                await _searchRepository.AddChunkAsync(chunk, link);
                x++;
                _logger.LogInformation($"Chunk {x} of {chunksCount} saved. Link num={i} url={link}");
            }
        }
    }

    private async Task<string> GetPageTextFromUrlAsync(string url)
    {
        var uri = new Uri(url);

        var title = uri.AbsolutePath
            .Replace("/ru/wiki/", "")
            .Replace("/wiki/", "");

        title = Uri.UnescapeDataString(title);

        var apiUrl =
            $"https://starwars.fandom.com/ru/api.php" +
            $"?action=query" +
            $"&prop=revisions" +
            $"&rvprop=content" +
            $"&rvslots=main" +
            $"&titles={Uri.EscapeDataString(title)}" +
            $"&format=json";

        using var client = new HttpClient();

        var json = await client.GetStringAsync(apiUrl);

        using var doc = JsonDocument.Parse(json);

        var pages = doc.RootElement
            .GetProperty("query")
            .GetProperty("pages");

        foreach (var page in pages.EnumerateObject())
        {
            var revisions = page.Value.GetProperty("revisions")[0];
            var content = revisions
                .GetProperty("slots")
                .GetProperty("main")
                .GetProperty("*")
                .GetString();

            return CleanWikiText(content);
        }

        return "";
    }

    private string CleanWikiText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return "";

        text = Regex.Replace(text, @"\{\{.*?\}\}", "", RegexOptions.Singleline);
        text = Regex.Replace(text, @"\[\[(?:[^|\]]*\|)?([^\]]+)\]\]", "$1");
        text = Regex.Replace(text, @"<.*?>", "");
        text = Regex.Replace(text, @"'{2,}", "");
        text = Regex.Replace(text, @"\s+", " ");

        return text.Trim();
    }
}