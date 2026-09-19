using System.Text.Json;
using System.Web;

namespace FringeApi;

/// <summary>
/// Defines a contract for types that can hydrate themselves from a JSON payload.
/// </summary>
/// <remarks>
/// Implementations use this method to merge the supplied JSON data into an existing object instance,
/// typically after deserializing a Fringe API response into a <see cref="JsonElement"/>.
/// This interface allows us to write code that works for both Shows and Venues.
/// </remarks>
internal interface IJsonUpdatable
{
    /// <summary>
    /// Updates the current instance using the provided JSON data.
    /// </summary>
    /// <param name="json">The JSON payload that contains the data to apply.</param>
    void UpdateFromJson(JsonElement json);
}

/// <summary>
/// Represents a single Fringe festival dataset and the in-memory collection of venues, shows, and performances
/// loaded from the Fringe API.
/// </summary>
/// <remarks>
/// This type acts as the aggregate root for festival data. It keeps track of the latest update timestamp,
/// downloads and merges API data, and exposes convenience queries for working with the dataset.
/// </remarks>
public sealed class Festival
{
    /// <summary>
    /// The canonical format used by the Fringe API for timestamps.
    /// </summary>
    internal const string DateFormat = "yyyy-MM-dd HH:mm:ss";

    /// <summary>
    /// The festival identifier used when creating the API client.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// The last time the festival data was successfully refreshed, with a 10-minute update buffer applied.
    /// </summary>
    public DateTime? LastUpdated { get; private set; }

    internal List<Performance> Performances { get; } = new();
    private Dictionary<string, Venue> VenuesById { get; } = new();
    private Dictionary<string, Venue> VenuesByCode { get; } = new();
    private Dictionary<string, Show> ShowsById { get; } = new();
    public Dictionary<string, Performance> PerformancesById { get; } = new();
    private ApiClient apiClient;

    /// <summary>
    /// Gets the number of shows currently in memory.
    /// </summary>
    public int ShowCount => ShowsById.Count;

    /// <summary>
    /// Gets the number of venues currently in memory.
    /// </summary>
    public int VenueCount => VenuesById.Count;

    /// <summary>
    /// Gets the number of performances currently in memory.
    /// </summary>
    public int PerformanceCount => Performances.Count;

    /// <summary>
    /// Initializes a new festival using the supplied Fringe API credentials and festival identifier.
    /// </summary>
    /// <param name="userId">The Fringe API user identifier.</param>
    /// <param name="apiKey">The Fringe API key.</param>
    /// <param name="festival">The festival name/code to query.</param>
    public Festival(string userId, string apiKey, string festival = "demofringe")
    {
        this.apiClient = new ApiClient(userId, apiKey, festival);
        this.Name = festival;
    }

    /// <summary>
    /// Parses a Fringe API datetime string into a <see cref="DateTime"/>.
    /// </summary>
    /// <param name="element">The JSON element containing the datetime string.</param>
    /// <returns>The parsed date and time in the festival's expected format.</returns>
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

    /// <summary>
    /// Refreshes the festival data from the Fringe API, merging new or modified venues and shows.
    /// </summary>
    /// <returns>
    /// A tuple containing the number of venue and show records processed during the refresh.
    /// </returns>
    /// <remarks>
    /// The API is queried in paginated chunks. If this festival has been updated before, the request only asks
    /// for records modified after the last successful refresh, allowing for the standard 10-minute buffering window.
    /// <para>We use the IJsonUpdatable interface to allow us to share code between venues and shows when processing JSON updates.</para>
    /// </remarks>
    public async Task<(int venueUpdatesCount, int showUpdatesCount)> UpdateFromFringeDataset()
    {
        int ProcessItemUpdates<T>(JsonDocument itemsJson, Dictionary<string, T> itemsById, Func<string, T> createAndRecordItem)
            where T : IJsonUpdatable
        {
            int count = 0;
            foreach (var itemJson in itemsJson.RootElement.EnumerateArray())
            {
                if (itemJson.TryGetProperty("id", out var itemIdProperty))
                {
                    var itemId = itemIdProperty.GetString();
                    if (itemId == null) continue; // This should never happen
                    if (itemsById.ContainsKey(itemId))
                    {
                        itemsById[itemId].UpdateFromJson(itemJson);
                    }
                    else
                    {
                        T item = createAndRecordItem(itemId);
                        item.UpdateFromJson(itemJson);
                    }
                    count++;
                }
                // The "else" should never happen because every item should have an "id" property
            }
            return count;
        }

        Venue CreateAndRecordVenue(string id)
        {
            var venue = new Venue() { Festival = this, Id = id };
            VenuesById[venue.Id] = venue;
            if (!string.IsNullOrEmpty(venue.Code))
            {
                VenuesByCode[venue.Code] = venue;
            }
            return venue;
        }

        Show CreateAndRecordShow(string id)
        {
            var show = new Show() { Festival = this, Id = id };
            ShowsById[show.Id] = show;
            return show;
        }

        async Task<int> FetchAndProcessUpdates(string endpoint, string filter, Func<JsonDocument, int> processUpdates)
        {
            // For both, loop fetching a chunk at a time. The Fringe API by default returns paginated
            // results in chunks of 25. We can specify a larger chunk size, up to 100. 
            // The start index of the first chunk is 0, not 1.
            var start = 0;
            var chunkSize = 100;
            int count  = 0;
            int thisCount;
            do
            {
                var argsWithPagination = $"{filter}from={start}&size={chunkSize}";
                using (var json = await apiClient.GetJsonAsync(endpoint, argsWithPagination))
                {
                    count += processUpdates(json);
                    start += chunkSize;
                    thisCount = json.RootElement.GetArrayLength();
                }
            } while (thisCount == chunkSize);
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
        var processVenueUpdates = (JsonDocument json) => ProcessItemUpdates<Venue>(json, VenuesById, CreateAndRecordVenue);
        var processShowUpdates = (JsonDocument json) => ProcessItemUpdates<Show>(json, ShowsById, CreateAndRecordShow);
        int venueUpdatesCount = await FetchAndProcessUpdates("venues", args, processVenueUpdates);
        int showUpdatesCount = await FetchAndProcessUpdates("events", args, processShowUpdates);

        return (venueUpdatesCount, showUpdatesCount);
    }

    /// <summary>
    /// Adds a venue to the festival if it has not already been registered.
    /// </summary>
    /// <param name="venue">The venue to add.</param>
    internal void AddVenue(Venue venue)
    {
        // Venue ID and code are required and immutable
        if (!VenuesById.ContainsKey(venue.Id))
        {
            VenuesById[venue.Id] = venue;
            VenuesByCode[venue.Code] = venue;
        }
    }

    /// <summary>
    /// Adds a show to the festival if it has not already been registered.
    /// </summary>
    /// <param name="show">The show to add.</param>
    internal void AddShow(Show show)
    {
        // Show ID is required and immutable
        if (!ShowsById.ContainsKey(show.Id))
        {
            ShowsById[show.Id] = show;
        }
    }

    /// <summary>
    /// Adds a performance to the festival if it has not already been registered.
    /// </summary>
    /// <param name="performance">The performance to add.</param>
    internal void AddPerformance(Performance performance)
    {
        // Performance ID is required and immutable
        if (!PerformancesById.ContainsKey(performance.Id))
        {
            Performances.Add(performance);
            PerformancesById[performance.Id] = performance;
        }
    }

    /// <summary>
    /// Removes a performance from the festival's in-memory collection.
    /// </summary>
    /// <param name="performance">The performance to remove.</param>
    internal void DropPerformance(Performance performance)
    {
        PerformancesById.Remove(performance.Id);
        Performances.Remove(performance);
    }

    /// <summary>
    /// Returns all shows that have at least one performance in the specified date range.
    /// </summary>
    /// <param name="start">The inclusive start of the date window.</param>
    /// <param name="end">The inclusive end of the date window.</param>
    /// <param name="genre">Optional genre filter.</param>
    /// <param name="venueName">Optional venue name filter.</param>
    /// <returns>A list of matching shows ordered by title.</returns>
    public List<Show> GetShowsByDate(DateTime start, DateTime end, string? genre = null, string? venueName = null)
    {
        if (end < start)
        {
            (start, end) = (end, start);
        }

        return ShowsById.Values
            .Where(show => show.Performances.Any(performance => performance.Start >= start && performance.Start <= end))
            .Where(show => string.IsNullOrWhiteSpace(genre) || string.Equals(show.Genre, genre, StringComparison.OrdinalIgnoreCase))
            .Where(show => string.IsNullOrWhiteSpace(venueName) || string.Equals(show.Venue?.Name, venueName, StringComparison.OrdinalIgnoreCase) || show.Performances.Any(p => string.Equals(p.Venue?.Name, venueName, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(show => show.Title)
            .ToList();
    }

    /// <summary>
    /// Gets performances occurring within a date range and close to a given coordinate.
    /// </summary>
    /// <param name="latitude">The latitude of the search point.</param>
    /// <param name="longitude">The longitude of the search point.</param>
    /// <param name="start">The start of the performance window.</param>
    /// <param name="end">The end of the performance window.</param>
    /// <param name="maxDistanceKm">The maximum permitted distance from the search location.</param>
    /// <param name="maxLeadTime">The maximum lead time before performance start.</param>
    /// <returns>A list of matching performances sorted by start time.</returns>
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

    /// <summary>
    /// Looks up a venue using its venue code.
    /// </summary>
    /// <param name="code">The venue code.</param>
    /// <returns>The matching venue, if found.</returns>
    public Venue? GetVenueByCode(string code)
    {
        VenuesByCode.TryGetValue(code, out var venue);
        return venue;
    }

    /// <summary>
    /// Looks up a venue by its internal unique identifier.
    /// </summary>
    /// <param name="id">The venue identifier.</param>
    /// <returns>The matching venue, if found.</returns>
    internal Venue? GetVenueById(string id)
    {
        VenuesById.TryGetValue(id, out var venue);
        return venue;
    }

    /// <summary>
    /// Finds the venues near the supplied location within the given distance threshold.
    /// </summary>
    /// <param name="position">The target position.</param>
    /// <param name="maxDistanceKm">The maximum allowed distance in kilometres.</param>
    /// <returns>The nearby venues, if any exist.</returns>
    public List<Venue> GetVenuesByLocation(Position position, double maxDistanceKm)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Serializes the festival and all associated venue/show data to the supplied stream as JSON.
    /// </summary>
    /// <param name="stream">The destination stream.</param>
    public void SaveToStream(Stream stream)
    {
        HashSet<string> skippedVenueKeys = new() { "id", "name", "address", "code", "position" };
        var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false });
        // Start the root object
        writer.WriteStartObject();
        // First write the festival header
        writer.WriteStartObject("festival");
        writer.WriteString("name", Name);
        if (LastUpdated.HasValue)
        {
            writer.WriteString("lastUpdated", LastUpdated.Value);
        }
        writer.WriteEndObject(); // end festival object

        // Now the list of venues
        writer.WriteStartArray("venues");
        foreach (var venue in VenuesById.Values)
        {
            writer.WriteStartObject();
            writer.WriteString("id", venue.Id);
            writer.WriteString("name", venue.Name);
            writer.WriteString("address", venue.Address);
            writer.WriteString("code", venue.Code);
            if (venue.Position != null)
            {
                writer.WriteStartObject("position");
                writer.WriteNumber("lat", venue.Position.Lat);
                writer.WriteNumber("lon", venue.Position.Lon);
                writer.WriteEndObject();
            }
            foreach (var kvp in venue.Extra ?? new Dictionary<string, JsonElement>())
            {
                if (skippedVenueKeys.Contains(kvp.Key) || kvp.Value.ValueKind == JsonValueKind.Null)
                    continue;
                writer.WritePropertyName(kvp.Key);
                kvp.Value.WriteTo(writer);
            }
            writer.WriteEndObject();
        }
        writer.WriteEndArray();

        // And finally the shows
        writer.WriteStartArray("shows");
        foreach (var show in ShowsById.Values)
        {
            writer.WriteStartObject();
            writer.WriteString("id", show.Id);
            writer.WriteString("status", show.Status.ToString().ToLower());
            writer.WriteString("title", show.Title);
            writer.WriteString("subtitle", show.Subtitle);
            writer.WriteString("genre", show.Genre);
            writer.WriteString("description", show.Description);
            writer.WriteStartObject("venue");
            writer.WriteString("id", show.Venue?.Id);
            writer.WriteEndObject();
            foreach (var kvp in show.Extra ?? new Dictionary<string, JsonElement>())
            {
                if (kvp.Value.ValueKind == JsonValueKind.Null || Show.SkippedKeys.Contains(kvp.Key))
                    continue;
                writer.WritePropertyName(kvp.Key);
                kvp.Value.WriteTo(writer);
            }
            writer.WriteStartArray("performances");
            foreach (var performance in show.Performances)
            {
                writer.WriteStartObject();
                writer.WriteString("id", performance.Id);
                writer.WriteString("start", performance.Start.ToString(DateFormat));
                writer.WriteString("end", performance.End.ToString(DateFormat));
                foreach (var kvp in performance.Extra ?? new Dictionary<string, JsonElement>())
                {
                    if (kvp.Value.ValueKind == JsonValueKind.Null || Performance.SkippedKeys.Contains(kvp.Key))
                        continue;
                    writer.WritePropertyName(kvp.Key);
                    kvp.Value.WriteTo(writer);
                }
                writer.WriteEndObject();
            }
            writer.WriteEndArray(); // End the array of performances
            writer.WriteEndObject(); // End the show
        }
        writer.WriteEndArray();

        writer.WriteEndObject(); // end root object
        writer.Flush();
    }

    /// <summary>
    /// Deserializes a festival instance from a previously saved JSON stream.
    /// </summary>
    /// <param name="stream">The source stream containing festival data.</param>
    /// <returns>The loaded festival.</returns>
    public static Festival LoadFromStream(Stream stream)
    {
        throw new NotImplementedException();
    }
}
