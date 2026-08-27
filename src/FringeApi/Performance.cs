namespace FringeApi;

using System.Text.Json;
using System.Text.Json.Serialization;

public class Performance
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public TimeSpan Time { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public Show? Show { get; set; }
    public Venue? Venue { get; set; }

    public DateTime Start => Date.Date.Add(Time);
    public DateTime End => Start.AddMinutes(90);

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Extra { get; set; }
}
