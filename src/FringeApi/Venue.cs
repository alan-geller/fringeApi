namespace FringeApi;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Represents a venue associated with a festival.
/// </summary>
public class Venue : IJsonUpdatable
{
    /// <summary>
    /// Gets the unique identifier for the venue.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the display name of the venue.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the street address for the venue.
    /// </summary>
    public string Address { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the venue code used by the festival data source.
    /// </summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the geographic coordinates of the venue, if available.
    /// </summary>
    public Position? Position { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp of the last update received for this venue.
    /// </summary>
    public DateTime LastUpdated { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the festival that owns this venue.
    /// </summary>
    public required Festival Festival { get; init; }

    /// <summary>
    /// Gets the set of JSON property names that should be ignored when storing extension data.
    /// </summary>
    internal static HashSet<string> SkippedKeys { get; } = new() { "id", "name", "address", "code", "position" };

    /// <summary>
    /// Gets the collection of shows scheduled at this venue.
    /// </summary>
    public HashSet<Show> Shows { get; } = new();

    /// <summary>
    /// Gets additional JSON fields that do not map to strongly typed properties.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Extra { get; private set; }

    /// <summary>
    /// Updates the venue properties from a JSON representation.
    /// </summary>
    /// <param name="venueJson">The JSON element containing the venue data.</param>
    /// <exception cref="InvalidOperationException">Thrown when the JSON payload contains a different venue identifier than the current instance.</exception>
    public void UpdateFromJson(JsonElement venueJson)
    {
        if (venueJson.TryGetProperty("id", out var id))
        {
            if (Id != id.GetString())
            {
                throw new InvalidOperationException($"Mismatched venue Id on update: existing Id = {Id}, new Id = {id.GetString()}");
            }
        }
        if (venueJson.TryGetProperty("name", out var nameProperty))
        {
            Name = nameProperty.GetString() ?? string.Empty;
        }
        if (venueJson.TryGetProperty("address", out var addressProperty))
        {
            Address = addressProperty.GetString() ?? string.Empty;
        }
        if (venueJson.TryGetProperty("code", out var codeProperty))
        {
            Code = codeProperty.GetString() ?? string.Empty;
        }
        if (venueJson.TryGetProperty("position", out var positionProperty) && positionProperty.ValueKind == JsonValueKind.Object)
        {
            var lat = positionProperty.GetProperty("lat").GetDouble();
            var lon = positionProperty.GetProperty("lon").GetDouble();
            Position = new Position { Lat = lat, Lon = lon };
        }
        foreach (var kvp in venueJson.EnumerateObject())
        {
            if (SkippedKeys.Contains(kvp.Name) || kvp.Value.ValueKind == JsonValueKind.Null)
                continue;
            Extra ??= new Dictionary<string, JsonElement>();
            Extra[kvp.Name] = kvp.Value.Clone();
        }
        LastUpdated = DateTime.UtcNow;
    }
}

/// <summary>
/// Represents a latitude and longitude pair for a venue location.
/// </summary>
public class Position
{
    /// <summary>
    /// Gets or sets the latitude value.
    /// </summary>
    public double Lat { get; set; }

    /// <summary>
    /// Gets or sets the longitude value.
    /// </summary>
    public double Lon { get; set; }
}
