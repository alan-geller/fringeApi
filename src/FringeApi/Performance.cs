namespace FringeApi;

using System.Text.Json;
using System.Text.Json.Serialization;

public class Performance
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string? Title { get; set; }
    public double Price { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Extra { get; set; }
}
