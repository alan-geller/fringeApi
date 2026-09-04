using System.Text.Json;
using FringeApi;
using Microsoft.Extensions.Configuration;



var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();
var apiKey = config["apiKey"] ?? "";

var festival = new Festival("5Jb4HXteE1tG8UWA", apiKey);

await festival.UpdateFromFringeDataset();

Console.WriteLine("Festival updated successfully.");
Console.WriteLine($"{festival.ShowCount} shows, {festival.VenueCount} venues, {festival.PerformanceCount} performances.");

// var client = new ApiClient("5Jb4HXteE1tG8UWA", apiKey, "demofringe");

// var shows = await client.GetDataAsync("events", "");

// var json = JsonDocument.Parse(shows);

// HashSet<string> performanceTags = new();

// foreach (var show in json.RootElement.EnumerateArray())
// {
// 	var performances = show.GetProperty("performances").EnumerateArray();
// 	foreach (var performance in performances)
// 	{
// 		if (performance.GetProperty("id").GetString() != null)
// 		{
// 			foreach (var property in performance.EnumerateObject())
// 			{
// 				// performanceTags.Add(property.Name);
// 			}
// 		}
// 		else
// 		{
// 			Console.WriteLine("Performance missing ID");
// 		}
// 	}
// }

// Console.WriteLine("Tags for Performances:");
// foreach (var tag in performanceTags)
// {
//     Console.WriteLine(tag);
// }

/* var venues = await client.GetDataAsync("venues", "");

var json = JsonDocument.Parse(venues);

File.WriteAllText("venues.json", JsonSerializer.Serialize(json.RootElement, new JsonSerializerOptions { WriteIndented = true }));
// Console.WriteLine(JsonSerializer.Serialize(json.RootElement, new JsonSerializerOptions { WriteIndented = true }));

HashSet<string> venueTags = new();
HashSet<string> venueIds = new();
HashSet<string> venueCodes = new();
foreach (var venue in json.RootElement.EnumerateArray())
{
	bool hasId = false;
	bool hasCode = false;
	bool hasSpaces = false;
    foreach (var property in venue.EnumerateObject())
    {
        venueTags.Add(property.Name);
        if ((property.Name == "id") && (property.Value.ValueKind == JsonValueKind.String))
        {
            hasId = true;
            if (!venueIds.Add(property.Value.GetString() ?? ""))
            {
				Console.WriteLine($"Duplicate venue ID found: {property.Value.GetString() ?? ""}");
            }
        }
        if ((property.Name == "code") && (property.Value.ValueKind == JsonValueKind.String))
        {
            hasCode = true;
            if (!venueCodes.Add(property.Value.GetString() ?? ""))
            {
				Console.WriteLine($"Duplicate venue code found: {property.Value.GetString() ?? ""}");
            }
        }
		if (property.Name == "performance_spaces" && property.Value.ValueKind == JsonValueKind.Array)
		{
			hasSpaces = true;
			if (property.Value.GetArrayLength() == 0)
			{
				Console.WriteLine($"Venue {venue.GetProperty("name").GetString() ?? ""} has an empty performance spaces array");
			}
			else if (property.Value.GetArrayLength() > 1)
			{
				Console.WriteLine($"Venue {venue.GetProperty("name").GetString() ?? ""} has more than one performance space");
			}
		}
    }
	if (!hasId)
	{
		Console.WriteLine($"Venue {venue.GetProperty("name").GetString() ?? ""} missing ID");
	}
	if (!hasCode)
	{
		Console.WriteLine($"Venue {venue.GetProperty("name").GetString() ?? ""} missing code");
	}
	if (!hasSpaces)
	{
		Console.WriteLine($"Venue {venue.GetProperty("name").GetString() ?? ""} missing performance spaces");
	}
}

Console.WriteLine("Tags for Venues:");
foreach (var tag in venueTags)
{
    Console.WriteLine(tag);
}
 */


// var venue = new Venue
// {
//     Name = "The Venue",
//     Address = "1 Festival Street, Edinburgh",
//     Code = "TV",
//     LastUpdated = new DateTime(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc),
//     Extra = new Dictionary<string, JsonElement> { ["capacity"] = "120" }
// };

// var show = new Show
// {
//     Title = "Example Show",
//     Genre = "Comedy",
//     Performer = "Demo Comic",
//     Description = "A test show.",
//     Venue = venue,
//     LastUpdated = new DateTime(2026, 8, 1, 9, 30, 0, DateTimeKind.Utc),
//     Extra = new Dictionary<string, JsonElement> { ["tag"] = "preview" }
// };

// var performance = new Performance
// {
//     Show = show,
//     Venue = venue,
//     Date = new DateTime(2026, 8, 5, 0, 0, 0, DateTimeKind.Utc),
//     Time = new TimeSpan(19, 30, 0),
//     LastUpdated = new DateTime(2026, 8, 1, 11, 0, 0, DateTimeKind.Utc),
//     Extra = new Dictionary<string, JsonElement> { ["status"] = "booked" }
// };

// show.Performances.Add(performance);
// venue.Performances.Add(performance);

// var festival = new Festival
// {
//     Name = "Edinburgh Fringe",
//     Location = "Edinburgh",
//     LastUpdated = new DateTime(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc),
//     Extra = new Dictionary<string, JsonElement> { ["city"] = "Edinburgh" }
// };

// festival.UpdateFromFringeDataset(
//     new[] { show },
//     new[] { performance },
//     new[] { venue });

// var showsInRange = festival.GetShowsByDate(
//     new DateTime(2026, 8, 5, 0, 0, 0, DateTimeKind.Utc),
//     new DateTime(2026, 8, 6, 0, 0, 0, DateTimeKind.Utc),
//     genre: "Comedy",
//     venueName: "The Venue");

// if (showsInRange.Count != 1)
// {
//     throw new InvalidOperationException($"Expected 1 show in range, got {showsInRange.Count}.");
// }

// var nearby = festival.GetNearbyPerformances(
//     55.95,
//     -3.18,
//     new DateTime(2026, 8, 5, 18, 0, 0, DateTimeKind.Utc),
//     new DateTime(2026, 8, 5, 23, 0, 0, DateTimeKind.Utc),
//     maxDistanceKm: 25,
//     maxLeadTime: TimeSpan.FromHours(6));

// if (nearby.Count != 1)
// {
//     throw new InvalidOperationException($"Expected 1 nearby performance, got {nearby.Count}.");
// }

// Console.WriteLine("Festival design regression checks passed.");
