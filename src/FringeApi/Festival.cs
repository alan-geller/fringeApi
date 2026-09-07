using System.Text.Json;
using System.Web;

namespace FringeApi;

public sealed class Festival
{
    internal const string DateFormat = "yyyy-MM-dd HH:mm:ss";

    public string Name { get; private set; } = string.Empty;
    public DateTime? LastUpdated { get; private set; }
    public Dictionary<string, string> Extra { get; private set; } = new();

    private List<Show> Shows { get; } = new();
    internal List<Performance> Performances { get; } = new();
    private List<Venue> Venues { get; } = new();
    private Dictionary<string, Venue> VenuesById { get; } = new();
    private Dictionary<string, Venue> VenuesByCode { get; } = new();
    private Dictionary<string, Show> ShowsById { get; } = new();
    public Dictionary<string, Performance> PerformancesById { get; } = new();
    private ApiClient apiClient;

    public int ShowCount => Shows.Count;
    public int VenueCount => Venues.Count;
    public int PerformanceCount => Performances.Count;

    public Festival(string userId, string apiKey, string festival = "demofringe")
    {
        this.apiClient = new ApiClient(userId, apiKey, festival);
        this.Name = festival;
    }

    internal static DateTime ParseFringeDateTime(JsonElement element)
    {
        var s = element.GetString() ?? string.Empty;
        return DateTime.ParseExact(s, DateFormat, null);
    }

    private async Task<(JsonDocument, JsonDocument)> FetchJsonUpdates(string args)
    {
        var showsJson = await apiClient.GetJsonAsync("events", args);
        var venuesJson = await apiClient.GetJsonAsync("venues", args);
        return (showsJson, venuesJson);
    }

    public async Task<(int venueUpdatesCount, int showUpdatesCount)> UpdateFromFringeDataset()
    {
        int ProcessVenueUpdates(JsonDocument venuesJson)
        {
            int count = 0;
            foreach (var venueJson in venuesJson.RootElement.EnumerateArray())
            {
                if (venueJson.TryGetProperty("id", out var venueIdProperty))
                {
                    var venueId = venueIdProperty.GetString();
                    if (venueId == null) continue; // This should never happen
                    if (VenuesById.ContainsKey(venueId))
                    {
                        VenuesById[venueId].UpdateFromJson(venueJson);
                    }
                    else
                    {
                        var venue = new Venue() { Festival = this, Id = venueId };
                        venue.UpdateFromJson(venueJson);
                        AddVenue(venue);
                    }
                    count++;
                }
                // The "else" should never happen because every venue should have an "id" property
            }
            return count;
        }

        int ProcessShowUpdates(JsonDocument showsJson)
        {
            int count = 0;
            foreach (var showJson in showsJson.RootElement.EnumerateArray())
            {
                if (showJson.TryGetProperty("id", out var showIdProperty))
                {
                    var showId = showIdProperty.GetString();
                    if (showId == null) continue; // This should never happen
                    if (ShowsById.ContainsKey(showId))
                    {
                        ShowsById[showId].UpdateFromJson(showJson);
                    }
                    else
                    {
                        var show = new Show() { Festival = this, Id = showId };
                        show.UpdateFromJson(showJson);
                        AddShow(show);
                    }
                    count++;
                }
                // The "else" should never happen because every show should have an "id" property
            }
            return count;
        }

        async Task<int> FetchAndProcessUpdates(string endpoint, string filter, Func<JsonDocument, int> processUpdates)
        {
            // For both, loop fetching a chunk at a time. The Fringe API by default returns paginated
            // results in chunks of 25. We can specify a larger chunk size, up to 100. 
            // The start index of the first chunk is 0, not 1.
            var start = 0;
            var chunkSize = 100;
            int count  = 0;
            JsonDocument json;
            do
            {
                var argsWithPagination = $"{filter}from={start}&size={chunkSize}";
                json = await apiClient.GetJsonAsync(endpoint, argsWithPagination);
                count += processUpdates(json);
                start += chunkSize;
            } while (json.RootElement.GetArrayLength() == chunkSize);
            return count;
        }

        var args = "";
        if (LastUpdated.HasValue)
        {
            // Note that Edinburgh is in the GMT timezone, so the date should be in UTC
            var date = HttpUtility.UrlEncode(LastUpdated.Value.ToString(DateFormat));
            // Append the modified_from filter to the API request
            args = $"modified_from={date}&";
        }
        // Fringe API docs suggests always allowing a 10 minute buffer for updates
        LastUpdated = DateTime.UtcNow.AddMinutes(-10);

        // First process venues, then shows.
        int venueUpdatesCount = await FetchAndProcessUpdates("venues", args, ProcessVenueUpdates);
        int showUpdatesCount = await FetchAndProcessUpdates("events", args, ProcessShowUpdates);

        return (venueUpdatesCount, showUpdatesCount);
    }

    internal void AddVenue(Venue venue)
    {
        // Venue ID and code are required and immutable
        if (!VenuesById.ContainsKey(venue.Id))
        {
            Venues.Add(venue);
            VenuesById[venue.Id] = venue;
            VenuesByCode[venue.Code] = venue;
        }
    }

    internal void AddShow(Show show)
    {
        // Show ID is required and immutable
        if (!ShowsById.ContainsKey(show.Id))
        {
            ShowsById[show.Id] = show;
            Shows.Add(show);
        }
    }

    internal void AddPerformance(Performance performance)
    {
        // Performance ID is required and immutable
        if (!PerformancesById.ContainsKey(performance.Id))
        {
            Performances.Add(performance);
            PerformancesById[performance.Id] = performance;
        }
    }

    internal void DropPerformance(Performance performance)
    {
        PerformancesById.Remove(performance.Id);
        Performances.Remove(performance);
    }

    public List<Show> GetShowsByDate(DateTime start, DateTime end, string? genre = null, string? venueName = null)
    {
        if (end < start)
        {
            (start, end) = (end, start);
        }

        return Shows
            .Where(show => show.Performances.Any(performance => performance.Start >= start && performance.Start <= end))
            .Where(show => string.IsNullOrWhiteSpace(genre) || string.Equals(show.Genre, genre, StringComparison.OrdinalIgnoreCase))
            .Where(show => string.IsNullOrWhiteSpace(venueName) || string.Equals(show.Venue?.Name, venueName, StringComparison.OrdinalIgnoreCase) || show.Performances.Any(p => string.Equals(p.Venue?.Name, venueName, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(show => show.Title)
            .ToList();
    }

    public List<Performance> GetNearbyPerformances(double latitude, double longitude, DateTime start, DateTime end, double maxDistanceKm, TimeSpan maxLeadTime)
    {
        if (end < start)
        {
            (start, end) = (end, start);
        }

        return Performances
            .Where(p => p.Start >= start && p.Start <= end)
            .Where(p => p.Venue != null)
            .Where(p =>
            {
                if (p.Venue is null) return false;
                if (string.IsNullOrWhiteSpace(p.Venue.Code) && string.IsNullOrWhiteSpace(p.Venue.Address))
                {
                    return true;
                }

                return true;
            })
            .OrderBy(p => p.Start)
            .ToList();
    }

    public Venue? GetVenueByCode(string code)
    {
        VenuesByCode.TryGetValue(code, out var venue);
        return venue;
    }
    
    internal Venue? GetVenueById(string id)
    {
        VenuesById.TryGetValue(id, out var venue);
        return venue;
    }

    public Venue? GetVenueByLocation(Position position, double maxDistanceKm)
    {
        throw new NotImplementedException();
    }
}
