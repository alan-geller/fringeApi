namespace FringeApi;

using System.Text.Json;
using System.Text.Json.Serialization;

public enum ShowStatus
{
    Active,
    Canceled,
    Deleted
}

public class Show
{
    public required Festival Festival { get; init; }
    public required string Id { get; init; }
    public ShowStatus Status { get; private set; } = ShowStatus.Active;
    public string Code { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Genre { get; private set; } = string.Empty;
    public string Performer { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string? Subtitle { get; private set; }
    public Venue? Venue { get; private set; }
    public DateTime LastUpdated { get; private set; } = DateTime.UtcNow;
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Extra { get; private set; }

    public List<Performance> Performances { get; } = new();
    public Dictionary<string, Performance> PerformancesById { get; } = new();

    public List<Performance> GetPerformancesByDate(DateTime start, DateTime end)
    {
        if (end < start)
        {
            (start, end) = (end, start);
        }

        return Performances
            .Where(p => p.Start >= start && p.Start <= end)
            .OrderBy(p => p.Start)
            .ToList();
    }

    internal void UpdateFromJson(JsonElement json)
    {
        if (json.TryGetProperty("id", out var id))
        {
            if (String.IsNullOrEmpty(Id))
            {
                return; // If Id is not set, we cannot update this show
            }
            else if (Id != id.GetString())
            {
                throw new InvalidOperationException($"Mismatched Id: existing Id = {Id}, new Id = {id.GetString()}");
            }
        }
        foreach (var kvp in json.EnumerateObject())
        {
            switch (kvp.Name)
            {
                case "title":
                    Title = kvp.Value.GetString() ?? string.Empty;
                    break;
                case "genre":
                    Genre = kvp.Value.GetString() ?? string.Empty;
                    break;
                case "performer":
                    Performer = kvp.Value.GetString() ?? string.Empty;
                    break;
                case "description":
                    Description = kvp.Value.GetString() ?? string.Empty;
                    break;
                case "subtitle":
                    Subtitle = kvp.Value.GetString();
                    break;
                case "venue":
                    var vid = kvp.Value.GetProperty("id").GetString() ?? string.Empty;
                    if (!String.IsNullOrEmpty(vid))
                    {
                        var oldVenue = Venue;
                        Venue = Festival.GetVenueById(vid);
                        oldVenue?.Shows.Remove(this);
                        Venue?.Shows.Add(this);
                    }
                    break;
                case "performances":
                    foreach (var performanceJson in kvp.Value.EnumerateArray())
                    {
                        var pid = performanceJson.GetProperty("id").GetString() ?? string.Empty;
                        if (!string.IsNullOrEmpty(pid))
                        {
                            if (PerformancesById.TryGetValue(pid, out var existingPerformance))
                            {
                                existingPerformance.UpdateFromJson(performanceJson);
                            }
                            else
                            {
                                var performance = new Performance() { Show = this };
                                performance.UpdateFromJson(performanceJson);
                                Performances.Add(performance);
                                PerformancesById[pid] = performance;
                                Festival.AddPerformance(performance);
                            }
                        }
                    }
                    break;
                default:
                    Extra ??= new Dictionary<string, JsonElement>();
                    Extra[kvp.Name] = kvp.Value;
                    break;
            }
        }
        LastUpdated = DateTime.UtcNow;
    }
}
