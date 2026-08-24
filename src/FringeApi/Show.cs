namespace FringeApi;

using System.Text.Json;
using System.Text.Json.Serialization;

public class Show
{
    public string Id { get; set; } = string.Empty;
    public string FestivalId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? SubTitle { get; set; }
    public Venue? Venue { get; set; }
    public Performance[] Performances { get; set; } = [];

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Extra { get; set; }
}
