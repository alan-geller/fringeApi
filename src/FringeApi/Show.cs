namespace FringeApi;

using System.Text.Json;
using System.Text.Json.Serialization;

public class Show
{
    public string Id { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public string Performer { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public Venue? Venue { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Extra { get; set; };

    public List<Performance> Performances { get; } = new();

    public List<Performance> GetPerformancesByDate(DateTime start, DateTime end)
    {
        if (end < start)
        {
            (start, end) = (end, start);
        }

        return Performances
            .Where(p => p.Date >= start && p.Date <= end)
            .OrderBy(p => p.Date)
            .ToList();
    }
}
