using System.Text;
using System.Text.Json;
using CivicPulse.Core.DTOs;
using CivicPulse.Core.Enums;
using CivicPulse.Core.Interfaces;

namespace CivicPulse.Web.Services;

public class AiCategorizationResult
{
    public string CategoryName { get; set; } = "";
    public int CategoryId { get; set; }
    public double Confidence { get; set; }
    public string Priority { get; set; } = "Medium";
    public string Method { get; set; } = "ai";
}

public class AiCategorizationService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ICategoryService _categoryService;
    private readonly ILogger<AiCategorizationService> _logger;

    private const string HF_API_URL =
        "https://api-inference.huggingface.co/models/facebook/bart-large-mnli";

    public AiCategorizationService(
        HttpClient http,
        IConfiguration config,
        ICategoryService categoryService,
        ILogger<AiCategorizationService> logger)
    {
        _http = http;
        _config = config;
        _categoryService = categoryService;
        _logger = logger;
    }

    public async Task<AiCategorizationResult?> CategorizeAsync(
        string title, string description)
    {
        var text = $"{title}. {description}".Trim();
        if (text.Length < 5) return null;

        var categories = await _categoryService.GetActiveAsync();
        if (!categories.Any()) return null;

        var categoryNames = categories.Select(c => c.Name).ToList();

        try
        {
            var hfResult = await CallHuggingFaceAsync(text, categoryNames);
            if (hfResult != null)
            {
                var matchedCat = categories.FirstOrDefault(c =>
                    c.Name == hfResult.Value.TopLabel);

                if (matchedCat != null)
                {
                    return new AiCategorizationResult
                    {
                        CategoryName = matchedCat.Name,
                        CategoryId = matchedCat.Id,
                        Confidence = hfResult.Value.TopScore,
                        Priority = matchedCat.DefaultPriority,
                        Method = "ai"
                    };
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("HuggingFace API failed: {msg}. Falling back to keywords.", ex.Message);
        }

        return KeywordFallback(text, categories);
    }

    private async Task<(string TopLabel, double TopScore)?> CallHuggingFaceAsync(
        string text, List<string> labels)
    {
        var token = _config["ApiKeys:HuggingFace"] ?? "";
        
        // Skip API call if token is missing or default
        if (string.IsNullOrWhiteSpace(token) || token == "YOUR_HUGGINGFACE_API_KEY")
        {
            _logger.LogWarning("HuggingFace API key not configured. Skipping external AI call.");
            return null;
        }

        var payload = new
        {
            inputs = text,
            parameters = new
            {
                candidate_labels = labels.ToArray(),
                multi_label = false
            }
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Add timeout so it doesn't hang
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        
        try 
        {
            var response = await _http.PostAsync(HF_API_URL, content, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("HF API returned {code}", response.StatusCode);
                return null;
            }

            var resultJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(resultJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("error", out var err))
            {
                _logger.LogWarning("HF API error: {err}", err.GetString());
                return null;
            }

            var labelsArray = root.GetProperty("labels").EnumerateArray().ToList();
            var scoresArray = root.GetProperty("scores").EnumerateArray().ToList();

            if (!labelsArray.Any()) return null;

            var topLabel = labelsArray[0].GetString() ?? "";
            var topScore = scoresArray[0].GetDouble();

            return (topLabel, topScore);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("HF API call failed: {msg}", ex.Message);
            return null;
        }
    }

    private AiCategorizationResult? KeywordFallback(
        string text, List<CategoryDto> categories)
    {
        var textLower = text.ToLower();

        string? bestCategory = null;
        int bestScore = 0;

        foreach (var cat in categories)
        {
            if (string.IsNullOrWhiteSpace(cat.Keywords)) continue;

            var keywords = cat.Keywords.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            int score = keywords.Count(k => textLower.Contains(k.ToLower()));

            if (score > bestScore)
            {
                bestScore = score;
                bestCategory = cat.Name;
            }
        }

        if (bestCategory == null || bestScore == 0) return null;

        var matched = categories.FirstOrDefault(c => c.Name == bestCategory);
        if (matched == null) return null;

        return new AiCategorizationResult
        {
            CategoryName = matched.Name,
            CategoryId = matched.Id,
            Confidence = Math.Min(0.5 + (bestScore * 0.1), 0.95),
            Priority = matched.DefaultPriority,
            Method = "keyword"
        };
    }
}
