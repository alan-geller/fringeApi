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

    internal static HashSet<string> SkippedKeys { get; } = new() { "id", "status", "title", "genre", "performer", 
                                                                    "description", "subtitle", "venue" };

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
                throw new InvalidOperationException($"Mismatched show Id: existing Id = {Id}, new Id = {id.GetString()}");
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
                case "status":
                    var status = kvp.Value.GetString() ?? string.Empty;
                    switch (status)
                    {
                        case "cancelled":
                            Status = ShowStatus.Canceled;
                            break;
                        case "deleted":
                            Status = ShowStatus.Deleted;
                            break;
                        default:
                            Status = ShowStatus.Active;
                            break;
                    }
                    break;
                case "performances":
                    // This has to be a bit complicated because the API always sends the full list of performances,
                    // so we need to either update existing performances or add new ones.
                    // Dropped performances just don't appear, so any missing ones should be removed from our list.
                    HashSet<string> seenPerformanceIds = new HashSet<string>();
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
                                var performance = new Performance() { Show = this, Id = pid };
                                performance.UpdateFromJson(performanceJson);
                                Performances.Add(performance);
                                PerformancesById[pid] = performance;
                                Festival.AddPerformance(performance);
                            }
                            seenPerformanceIds.Add(pid);
                        }
                    }
                    // Remove any performances that were not seen in the latest update
                    foreach (var performance in Performances.Where(p => !seenPerformanceIds.Contains(p.Id)).ToList())
                    {
                        PerformancesById.Remove(performance.Id);
                        Performances.Remove(performance);
                        Festival.DropPerformance(performance);
                    }
                    break;
                default:
                    Extra ??= new Dictionary<string, JsonElement>();
                    Extra[kvp.Name] = kvp.Value.Clone();
                    break;
            }
        }
        LastUpdated = DateTime.UtcNow;
    }
}
