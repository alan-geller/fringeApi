using System.Runtime.Serialization;
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

public enum EdinburghFestival
{
    DemoFringe,
    Fringe,
    Jazz,
    Book,
    International,
    Tattoo,
    Art,
    Hogmanay,
    Science,
    Imaginate,
    Film,
    Mela,
    Storytelling
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
    public required EdinburghFestival Name { get; init; }

    /// <summary>
    /// The last time the festival data was successfully refreshed, with a 10-minute update buffer applied.
    /// </summary>
    public DateTime? LastUpdated { get; private set; }

    /// <summary>
    /// Gets or sets the update margin in minutes used to determine if a refresh is needed.
    /// </summary>
    public int UpdateMarginInMinutes { get; set; } = 10;

    /// <summary>
    /// Gets or sets the interval at which festival data should be refreshed.
    /// </summary>
    public TimeSpan UpdateInterval { get; set; } = TimeSpan.FromMinutes(10);

    private Dictionary<string, Venue> VenuesById { get; } = [];
    private Dictionary<string, Venue> VenuesByCode { get; } = [];
    private Dictionary<string, Show> ShowsById { get; } = [];
    private Dictionary<string, Performance> PerformancesById { get; } = [];
    private ApiClient ApiClient { get; init; }
    private StreamWriter? logStream = null;

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
    public int PerformanceCount => PerformancesById.Count;

    /// <summary>
    /// Initializes a new festival using the supplied Fringe API credentials and festival identifier.
    /// </summary>
    /// <param name="userId">The Fringe API user identifier.</param>
    /// <param name="apiKey">The Fringe API key.</param>
    /// <param name="festival">The festival name/code to query.</param>
    public Festival(string userId, string apiKey, EdinburghFestival festival = EdinburghFestival.DemoFringe)
    {
        this.ApiClient = new ApiClient(userId, apiKey, festival.ToString().ToLower());
        this.Name = festival;
    }

    public void SetLogStream(StreamWriter logStream)
    {
        if (this.logStream != null)
        {
            this.logStream.Dispose();
        }
        this.logStream = logStream;
    }

    internal void Log(string message)
    {
        if (logStream != null)
        {
            logStream.WriteLine(message);
            logStream.Flush();
        }
    }

    internal void LogException(Exception ex, string context)
    {
        if (logStream != null)
        {
            logStream.WriteLine($"Exception {ex.Message} while '{context}'");
            logStream.Flush();
        }
    }

    /// <summary>
    /// Parses a Fringe API datetime string into a <see cref="DateTime"/>.
    /// </summary>
    /// <param name="element">The JSON element containing the datetime string.</param>
    /// <returns>The parsed date and time in the festival's expected format.</returns>
    internal static DateTime ParseFestivalDateTime(JsonElement element)
    {
        var s = element.GetString() ?? string.Empty;
        return DateTime.ParseExact(s, DateFormat, null);
    }

    /// <summary>
    /// Formats a <see cref="DateTime"/> for use in a Fringe API request parameter.
    /// </summary>
    /// <param name="dateTime">The date and time to encode.</param>
    /// <returns>
    /// The timestamp formatted using the festival's canonical API format and URL-encoded for query-string use.
    /// </returns>
    internal static string FormatFestivalDateTime(DateTime dateTime)
    {
        return HttpUtility.UrlEncode(dateTime.ToString(DateFormat));
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
        void ProcessItemUpdate<T>(JsonElement itemJson, Dictionary<string, T> itemsById,
            Func<string, T> createAndRecordItem) where T : IJsonUpdatable
        {
            if (itemJson.TryGetProperty("id", out var itemIdProperty))
            {
                var itemId = itemIdProperty.GetString();
                if (itemId == null) return; // This should never happen
                lock (itemsById)
                {
                    if (itemsById.ContainsKey(itemId))
                    {
                        itemsById[itemId].UpdateFromJson(itemJson);
                    }
                    else
                    {
                        T item = createAndRecordItem(itemId);
                        item.UpdateFromJson(itemJson);
                    }

                }
            }
            // The "else" should never happen because every item should have an "id" property
        }

        Venue CreateAndRecordVenue(string id)
        {
            // Note that VenuesById is already locked here
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
            // Note that ShowsById is already locked here
            var show = new Show() { Festival = this, Id = id };
            ShowsById[show.Id] = show;
            return show;
        }

        var args = "";
        if (LastUpdated.HasValue)
        {
            // Note that Edinburgh is in the GMT timezone, so the date should be in UTC
            var date = FormatFestivalDateTime(LastUpdated.Value.AddMinutes(-UpdateMarginInMinutes));
            // Append the modified_from filter to the API request
            args = $"modified_from={date}&";
        }

        // First process venues, then shows.
        try
        {
            var processVenueUpdate = (JsonElement json) => ProcessItemUpdate<Venue>(json, VenuesById, CreateAndRecordVenue);
            int venueUpdatesCount = await ApiClient.ProcessPagedJsonVoidAsync("venues", args, processVenueUpdate);
            var processShowUpdate = (JsonElement json) => ProcessItemUpdate<Show>(json, ShowsById, CreateAndRecordShow);
            int showUpdatesCount = await ApiClient.ProcessPagedJsonVoidAsync("events", args, processShowUpdate);
            LastUpdated = DateTime.UtcNow;
            return (venueUpdatesCount, showUpdatesCount);
        }
        catch (Exception ex)
        {
            // Only unrecoverable errors should reach this point
            LogException(ex, "fetching and processing updates");
            throw;
        }
    }

    /// <summary>
    /// Adds a venue to the festival if it has not already been registered.
    /// </summary>
    /// <param name="venue">The venue to add.</param>
    internal void AddVenue(Venue venue)
    {
        lock (VenuesById)
        {
            // Venue ID and code are required and immutable
            if (!VenuesById.ContainsKey(venue.Id))
            {
                VenuesById[venue.Id] = venue;
                if (!String.IsNullOrEmpty(venue.Code))
                {
                    VenuesByCode[venue.Code] = venue;
                }
            }
        }
    }

    /// <summary>
    /// Adds a performance to the festival if it has not already been registered.
    /// </summary>
    /// <param name="performance">The performance to add.</param>
    internal void AddPerformance(Performance performance)
    {
        lock (PerformancesById)
        {
            // Performance ID is required and immutable
            if (!PerformancesById.ContainsKey(performance.Id))
            {
                PerformancesById[performance.Id] = performance;
            }
        }
    }

    /// <summary>
    /// Removes a performance from the festival's in-memory collection.
    /// </summary>
    /// <param name="performance">The performance to remove.</param>
    internal void DropPerformance(Performance performance)
    {
        lock (PerformancesById)
        {
            PerformancesById.Remove(performance.Id);
        }
    }

    /// <summary>
    /// Retrieves a show by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the show to retrieve.</param>
    /// <returns>
    /// The matching <see cref="Show"/> instance if found; otherwise, <see langword="null"/>.
    /// </returns>
    public Show? GetShowById(string id)
    {
        lock (ShowsById)
        {
            if (ShowsById.TryGetValue(id, out var show))
            {
                return show;
            }
            return null;
        }
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

        List<Show> matchingShows;
        lock (ShowsById)
        {
            matchingShows = ShowsById.Values
                .Where(show => show.Performances.Any(performance => performance.Start >= start && performance.Start <= end))
                .Where(show => string.IsNullOrWhiteSpace(genre) || string.Equals(show.Genre, genre, StringComparison.OrdinalIgnoreCase))
                .Where(show => string.IsNullOrWhiteSpace(venueName) || string.Equals(show.Venue?.Name, venueName, StringComparison.OrdinalIgnoreCase) || show.Performances.Any(p => string.Equals(p.Venue?.Name, venueName, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }
        // Do the sorting outside of the lock
        return matchingShows.OrderBy(show => show.Title).ToList();
    }

    /// <summary>
    /// Gets performances occurring within a date range and close to a given coordinate.
    /// </summary>
    /// <param name="latitude">The latitude of the search point.</param>
    /// <param name="longitude">The longitude of the search point.</param>
    /// <param name="start">The start of the performance window.</param>
    /// <param name="end">The end of the performance window.</param>
    /// <param name="maxDistanceKm">The maximum permitted distance from the search location.</param>
    /// <returns>A list of matching performances sorted by start time.</returns>
    public async Task<List<Performance>> GetNearbyPerformancesAsync(Position position, DateTime? start, DateTime? end, int maxDistanceKm)
    {
        var query = $"lat={position.Lat}&lon={position.Lon}&distance={maxDistanceKm}kilometers";
        if (start.HasValue)
        {
            query += $"&date_from={FormatFestivalDateTime(start.Value)}";
        }
        if (end.HasValue)
        {
            query += $"&date_to={FormatFestivalDateTime(end.Value)}";
        }
        var json = await ApiClient.GetJsonAsync("events", query);

        var performances = new List<Performance>();
        foreach (var showElement in json.RootElement.EnumerateArray())
        {
            if (showElement.TryGetProperty("id", out var showIdElement))
            {
                var showId = showIdElement.GetString();
                if (!String.IsNullOrEmpty(showId))
                {
                    var show = GetShowById(showId);
                    if (show != null)
                    {
                        var performancesInRange = show.Performances
                            .Where(p => (!start.HasValue || p.Start >= start.Value) && (!end.HasValue || p.Start <= end.Value));
                        performances.AddRange(performancesInRange);
                    }
                }
            }
        }

        return [.. performances.OrderBy(p => p.Start)];
    }

    /// <summary>
    /// Looks up a venue using its venue code.
    /// </summary>
    /// <param name="code">The venue code.</param>
    /// <returns>The matching venue, if found.</returns>
    public Venue? GetVenueByCode(string code)
    {
        lock (VenuesById)
        {
            if (VenuesByCode.TryGetValue(code, out var venue))
            {
                return venue;
            }
            return null;
        }
    }

    /// <summary>
    /// Looks up a venue by its internal unique identifier.
    /// </summary>
    /// <param name="id">The venue identifier.</param>
    /// <returns>The matching venue, if found.</returns>
    public Venue? GetVenueById(string id)
    {
        lock (VenuesById)
        {
            if (VenuesById.TryGetValue(id, out var venue))
            {
                return venue;
            }
            return null;
        }
    }

    /// <summary>
    /// Finds the venues near the supplied location within the given distance threshold.
    /// </summary>
    /// <param name="position">The target position.</param>
    /// <param name="maxDistanceKm">The maximum allowed distance in kilometres.</param>
    /// <returns>The nearby venues, if any exist.</returns>
    public async Task<List<Venue>> GetVenuesByLocationAsync(Position position, int maxDistanceKm)
    {
        var query = $"lat={position.Lat}&lon={position.Lon}&distance={maxDistanceKm}kilometers";
        var json = await ApiClient.GetJsonAsync("venues", query);
        var venues = new List<Venue>();
        foreach (var venueElement in json.RootElement.EnumerateArray())
        {
            if (venueElement.TryGetProperty("id", out var venueIdElement))
            {
                var venueId = venueIdElement.GetString();
                if (!String.IsNullOrEmpty(venueId))
                {
                    var venue = GetVenueById(venueId);
                    if (venue != null)
                    {
                        venues.Add(venue);
                    }
                }
            }
        }
        return venues;
    }

    /// <summary>
    /// Serializes the festival and all associated venue/show data to the supplied stream as JSON.
    /// </summary>
    /// <param name="stream">The destination stream.</param>
    public void SaveToStream(Stream stream)
    {
        var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false });
        // Start the root object
        writer.WriteStartObject();
        // First write the festival header
        writer.WriteStartObject("festival");
        writer.WriteString("name", Name.ToString());
        if (LastUpdated.HasValue)
        {
            writer.WriteString("lastUpdated", LastUpdated.Value.ToString(DateFormat));
        }
        writer.WriteNumber("updateInterval", UpdateInterval.TotalMilliseconds);
        writer.WriteNumber("updateMarginInMinutes", UpdateMarginInMinutes);
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
                if (Venue.SkippedKeys.Contains(kvp.Key) || kvp.Value.ValueKind == JsonValueKind.Null)
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
    public void LoadFromStream(Stream stream)
    {
        using var doc = JsonDocument.Parse(stream);
        var root = doc.RootElement;

        // First, parse the basic Festival properties
        if (root.TryGetProperty("festival", out var festivalElement))
        {
            var name = festivalElement.GetProperty("name").GetString();
            if (name != this.Name.ToString())
            {
                throw new InvalidOperationException($"Festival name mismatch. Expected: {this.Name}, Found: {name}");
            }
            if (root.TryGetProperty("lastUpdated", out var lastUpdatedElement))
            {
                LastUpdated = Festival.ParseFestivalDateTime(lastUpdatedElement);
            }
            UpdateInterval = TimeSpan.FromMilliseconds(festivalElement.GetProperty("updateInterval").GetInt32());
            UpdateMarginInMinutes = festivalElement.GetProperty("updateMarginInMinutes").GetInt32();
        }

        // Now, vanues
        if (root.TryGetProperty("venues", out var venuesElement))
        {
            foreach (var venueElement in venuesElement.EnumerateArray())
            {
                string? id;
                if (venueElement.TryGetProperty("id", out var idElement) && ((id = idElement.GetString()) != null))
                {
                    var venue = new Venue() { Id = id, Festival = this };
                    venue.UpdateFromJson(venueElement);
                    VenuesById.Add(id, venue);
                }
                else
                {
                    throw new InvalidOperationException("Venue element is missing an 'id' property.");
                }
            }
        }

        // And finally shows
        if (root.TryGetProperty("shows", out var showsElement))
        {
            foreach (var showElement in showsElement.EnumerateArray())
            {
                string? id;
                if (showElement.TryGetProperty("id", out var idElement) && ((id = idElement.GetString()) != null))
                {
                    var show = new Show() { Id = id, Festival = this };
                    show.UpdateFromJson(showElement);
                    ShowsById.Add(id, show);
                }
                else
                {
                    throw new InvalidOperationException("Show element is missing an 'id' property.");
                }
            }
        }
    }

    /// <summary>
    /// Runs a background update loop that periodically refreshes festival data from the Fringe dataset.
    /// </summary>
    /// <param name="cancellationToken">A token to signal cancellation of the background update task.</param>
    /// <remarks>
    /// This method will run indefinitely until the cancellation token is triggered.
    /// Updates are performed at intervals defined by <see cref="UpdateInterval"/>.
    /// </remarks>
    public async Task BackgroundUpdateAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(UpdateInterval, cancellationToken);
            var _ = await UpdateFromFringeDataset();
        }
    }
}
