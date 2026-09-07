namespace FringeApi;

using System.Text.Json;
using System.Text.Json.Serialization;

public class Venue
{
    public required string Id { get; init; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
	public Position? Position { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
	public required Festival Festival { get; init; }
 
    public HashSet<Show> Shows { get; } = new();

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Extra { get; set; }

	internal void Update(Venue venue)
	{
		Name = venue.Name;
		Address = venue.Address;
		Code = venue.Code;
		Position = venue.Position;
		foreach (var kvp in venue.Extra ?? new Dictionary<string, JsonElement>())
		{
			Extra ??= new Dictionary<string, JsonElement>();
			Extra[kvp.Key] = kvp.Value;
		}
		LastUpdated = venue.LastUpdated;
	}

	internal void UpdateFromJson(JsonElement venueJson)
	{
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
			if (kvp.Name != "id" && kvp.Name != "name" && kvp.Name != "address" && kvp.Name != "code" && kvp.Name != "position")
			{
				Extra ??= new Dictionary<string, JsonElement>();
				Extra[kvp.Name] = kvp.Value;
			}
		}
		LastUpdated = DateTime.UtcNow;
	}


}

public class Position
{
    public double Lat { get; set; }
    public double Lon { get; set; }
}
