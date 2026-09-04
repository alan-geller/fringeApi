using System.Text.Json;

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

    private async Task<(IEnumerable<Show>, IEnumerable<Venue>)> FetchUpdates()
    {
        // Implementation for fetching updates from the API
        var filter = "";
        if (LastUpdated.HasValue)
        {
            var date = LastUpdated.Value.ToString(DateFormat);
            filter = $"modified_from={date}";
        }
        var shows = await apiClient.GetAndDeserializeAsync<Show>("events", filter);
        var venues = await apiClient.GetAndDeserializeAsync<Venue>("venues", filter);
        return (shows, venues);
    }

    private async Task<(JsonDocument, JsonDocument)> FetchJsonUpdates()
    {
        var showsJson = await apiClient.GetJsonAsync("events", "");
        var venuesJson = await apiClient.GetJsonAsync("venues", "");
        return (showsJson, venuesJson);
    }

    public async Task UpdateFromFringeDataset()
    {
        var (showsJson, venuesJson) = await FetchJsonUpdates();
        
        foreach (var venueJson in venuesJson.RootElement.EnumerateArray())
        {
            if (venueJson.TryGetProperty("id", out var venueIdProperty))
            {
                var venueId = venueIdProperty.GetString();
                if (venueId == null) continue; // This is an error in the response JSON
                if (VenuesById.ContainsKey(venueId))
                {
                    VenuesById[venueId].UpdateFromJson(venueJson);
                }
                else
                {
                    var venue = new Venue() { Festival = this, Id = venueId };
                    venue.UpdateFromJson(venueJson);
                    Venues.Add(venue);
                    VenuesById[venue.Id] = venue;
                    VenuesByCode[venue.Code] = venue;
                }
            }
        }
        foreach (var showJson in showsJson.RootElement.EnumerateArray())
        {
            if (showJson.TryGetProperty("id", out var showIdProperty))
            {
                var showId = showIdProperty.GetString();
                if (showId == null) continue; // This is an error in the response JSON
                if (ShowsById.ContainsKey(showId))
                {
                    ShowsById[showId].UpdateFromJson(showJson);
                }
                else
                {
                    var show = new Show() { Festival = this, Id = showId };
                    show.UpdateFromJson(showJson);
                    Shows.Add(show);
                    ShowsById[show.Id] = show;
                }
            }
        }
    }

    public async Task UpdateFromFringeDataset2()
    {
        var (fetchedShows, fetchedVenues) = await FetchUpdates();
        foreach (var venue in fetchedVenues)
        {
            if (VenuesById.ContainsKey(venue.Id)) {
                VenuesById[venue.Id].Update(venue);
            } else {
                Venues.Add(venue);
                VenuesByCode[venue.Code] = venue;
                VenuesById[venue.Id] = venue;
            }
        }

        foreach (var show in fetchedShows)
        {
            if (ShowsById.ContainsKey(show.Id)) {
                ShowsById[show.Id].Update(show);
            } else {
                Shows.Add(show);
                ShowsById[show.Id] = show;
            }
        }
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
