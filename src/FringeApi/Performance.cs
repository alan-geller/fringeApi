namespace FringeApi;

using System.Text.Json;
using System.Text.Json.Serialization;

public class Performance
{
    public string Id { get; set; } = string.Empty;
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public required Show Show { get; set; }
    public Venue? CustomVenue { get; set; }
    public Venue? Venue { get { return CustomVenue ?? Show.Venue; }  }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Extra { get; set; }

    public void UpdateFromJson(JsonElement json)
    {
        if (json.TryGetProperty("id", out var id))
        {
            if (String.IsNullOrEmpty(Id))
            {
                Id = id.GetString() ?? string.Empty;
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
                case "id":
                    continue;
                case "start":
                    Start = Festival.ParseFringeDateTime(kvp.Value);
                    continue;
                case "end":
                    End = Festival.ParseFringeDateTime(kvp.Value);
                    continue;
                // Venue specific for this performance? We don't know the tag
                default:
                    Extra ??= new Dictionary<string, JsonElement>();
                    Extra[kvp.Name] = kvp.Value;
                    break;
            }
        }
    }
}
