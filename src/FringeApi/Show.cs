namespace FringeApi;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Represents the current lifecycle state of a show returned by the festival API.
/// </summary>
public enum ShowStatus
{
    /// <summary>
    /// The show is active and currently available.
    /// </summary>
    Active,

    /// <summary>
    /// The show has been canceled.
    /// </summary>
    Canceled,

    /// <summary>
    /// The show has been removed from the festival data set.
    /// </summary>
    Deleted
}

/// <summary>
/// Represents a single show within a festival and owns the related performance data.
/// </summary>
public class Show : IJsonUpdatable
{
    /// <summary>
    /// Gets the festival that owns this show.
    /// </summary>
    public required Festival Festival { get; init; }

    /// <summary>
    /// Gets the unique identifier for this show.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the current status of the show.
    /// </summary>
    public ShowStatus Status { get; private set; } = ShowStatus.Active;

    /// <summary>
    /// Gets the show title.
    /// </summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the primary genre associated with the show.
    /// </summary>
    public string Genre { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the performer or artist name associated with the show.
    /// </summary>
    public string Performer { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the descriptive text for the show.
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the optional subtitle for the show.
    /// </summary>
    public string? Subtitle { get; private set; }

    /// <summary>
    /// Gets the venue where this show is performed, if available.
    /// </summary>
    public Venue? Venue { get; private set; }

    /// <summary>
    /// Gets the timestamp of the most recent update applied to this show.
    /// </summary>
    public DateTime LastUpdated { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets extra properties that are not explicitly mapped to a CLR property.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Extra { get; private set; }

    /// <summary>
    /// Gets the list of performances associated with this show.
    /// </summary>
    public List<Performance> Performances { get; } = new();

    /// <summary>
    /// Gets a lookup of performances by their unique identifier.
    /// </summary>
    public Dictionary<string, Performance> PerformancesById { get; } = new();

    /// <summary>
    /// Gets the set of JSON keys that should be ignored when materializing a show from API data.
    /// </summary>
    internal static HashSet<string> SkippedKeys { get; } = new() { "id", "status", "title", "genre", "performer",
                                                                    "description", "subtitle", "venue" };

    /// <summary>
    /// Returns all performances that fall within the specified date range.
    /// </summary>
    /// <param name="start">The inclusive lower bound of the range.</param>
    /// <param name="end">The inclusive upper bound of the range.</param>
    /// <returns>A sorted list of performances within the given range.</returns>
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

    /// <summary>
    /// Updates this show using a JSON payload from the festival API.
    /// </summary>
    /// <param name="json">The JSON element containing the updated show data.</param>
    /// <exception cref="InvalidOperationException">Thrown when the JSON payload contains a different show identifier than the current instance.</exception>
    public void UpdateFromJson(JsonElement json)
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
