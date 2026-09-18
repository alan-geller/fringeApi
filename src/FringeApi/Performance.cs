namespace FringeApi;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Represents a performance returned by the Fringe API.
/// </summary>
public class Performance
{
    /// <summary>
    /// Gets the performance identifier.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the performance start time.
    /// </summary>
    public DateTime Start { get; private set; }

    /// <summary>
    /// Gets the performance end time.
    /// </summary>
    public DateTime End { get; private set; }

    /// <summary>
    /// Gets the last time this performance was updated.
    /// </summary>
    public DateTime LastUpdated { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the associated show.
    /// </summary>
    public required Show Show { get; init; }

    /// <summary>
    /// Gets the venue for the performance, inherited from the associated show.
    /// </summary>
    public Venue? Venue { get => Show.Venue; }

    /// <summary>
    /// Gets additional unmodeled JSON properties.
    /// </summary>
    public Dictionary<string, JsonElement>? Extra { get; private set; }

    /// <summary>
    /// Gets the set of JSON keys that are skipped during update processing.
    /// </summary>
    internal static HashSet<string> SkippedKeys { get; } = new() { "id" };

    /// <summary>
    /// Updates the performance from a JSON payload.
    /// </summary>
    /// <param name="json">The JSON object containing updated values.</param>
    /// <exception cref="InvalidOperationException">Thrown when the JSON identifier does not match the existing performance identifier.</exception>
    internal void UpdateFromJson(JsonElement json)
    {
        if (json.TryGetProperty("id", out var id))
        {
            if (Id != id.GetString())
            {
                throw new InvalidOperationException($"Mismatched performance Id on update: existing Id = {Id}, new Id = {id.GetString()}");
            }
        }
        foreach (var kvp in json.EnumerateObject())
        {
            switch (kvp.Name)
            {
                case "id":
                    continue;
                case "start":
                    Start = Festival.ParseFringeDateTime(kvp.Value);
                    continue;
                case "end":
                    End = Festival.ParseFringeDateTime(kvp.Value);
                    continue;
                default:
                    Extra ??= new Dictionary<string, JsonElement>();
                    Extra[kvp.Name] = kvp.Value.Clone();
                    break;
            }
        }
    }
}
