using CivicPulse.Core.DTOs;
using CivicPulse.Core.Entities;
using CivicPulse.Core.Enums;
using CivicPulse.Core.Interfaces;

namespace CivicPulse.Web.Services;

public class CategorizationEngine
{
    private readonly IRepository<Category> _categoryRepo;

    public CategorizationEngine(IRepository<Category> categoryRepo)
    {
        _categoryRepo = categoryRepo;
    }

    public async Task<CategorizationResult> CategorizeAsync(string title, string description)
    {
        var text = $"{title} {description}".ToLowerInvariant();
        var categories = await _categoryRepo.FindAsync(c => c.IsActive);

        var scores = new List<(Category Category, double Score)>();

        foreach (var category in categories)
        {
            if (string.IsNullOrWhiteSpace(category.Keywords)) continue;

            var keywords = category.Keywords.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            double score = 0;

            foreach (var keyword in keywords)
            {
                var kw = keyword.ToLowerInvariant();
                if (title.ToLowerInvariant().Contains(kw)) score += 3;
                else if (description.ToLowerInvariant().Contains(kw)) score += 1;
                else if (text.Contains(kw)) score += 0.5;
            }

            if (score > 0)
                scores.Add((category, score));
        }

        if (scores.Count == 0)
            return new CategorizationResult { IsConfident = false, Confidence = 0 };

        var best = scores.OrderByDescending(s => s.Score).First();
        var maxPossibleScore = categories.Max(c => c.Keywords.Split(',', StringSplitOptions.RemoveEmptyEntries).Length * 3);
        var normalizedScore = maxPossibleScore > 0 ? best.Score / maxPossibleScore : 0;

        return new CategorizationResult
        {
            CategoryId = best.Category.Id,
            CategoryName = best.Category.Name,
            Confidence = Math.Min(normalizedScore, 1.0),
            IsConfident = normalizedScore >= 0.3
        };
    }

    public PriorityLevel AssignPriority(string title, string description, Category category)
    {
        var text = $"{title} {description}".ToLowerInvariant();

        var criticalKeywords = new[] { "gas leak", "collapse", "fire", "flood", "emergency",
                                       "accident", "electrocution", "dead", "dangerous" };
        var highKeywords = new[] { "broken", "fault", "outage", "overflow", "urgent", "blocked" };

        if (criticalKeywords.Any(k => text.Contains(k))) return PriorityLevel.Critical;
        if (highKeywords.Any(k => text.Contains(k))) return PriorityLevel.High;
        return category.DefaultPriority;
    }
}
