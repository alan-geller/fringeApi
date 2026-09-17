namespace FringeApi;

using System.Text.Json;
using System.Text.Json.Serialization;

public class Performance
{
    public required string Id { get; init; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public required Show Show { get; init; }
    public Venue? Venue { get => Show.Venue; }
    public Dictionary<string, JsonElement>? Extra { get; set; }

    public static HashSet<string> SkippedKeys { get; } = new() { "id" };

    public void UpdateFromJson(JsonElement json)
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
