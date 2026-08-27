namespace FringeApi;

using System.Text.Json;
using System.Text.Json.Serialization;

public class Venue
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
	public Position? Position { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
 
    public List<Performance> Performances { get; } = new();

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Extra { get; set; }
}

public class Position
{
    public double Lat { get; set; }
    public double Lon { get; set; }
}
