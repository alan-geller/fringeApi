using System.Text.Json;
using System.IO.Compression;
using FringeApi;
using Microsoft.Extensions.Configuration;


var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();
var apiKey = config["apiKey"] ?? "";

// var festival = new Festival("5Jb4HXteE1tG8UWA", apiKey) { Name = EdinburghFestival.DemoFringe };
// var logStream = File.CreateText("festival.log");
// festival.SetLogStream(logStream);

// using var mergedStream = File.OpenRead("merged.json.gz");
// using var gzipStream = new GZipStream(mergedStream, CompressionMode.Decompress);
// festival.LoadFromStream(gzipStream);
// gzipStream.Close();
// mergedStream.Close();

// var (venueUpdatesCount, showUpdatesCount) = await festival.UpdateFromFringeDataset();

// Console.WriteLine("Festival loaded successfully.");
// // Console.WriteLine($"{venueUpdatesCount} venue updates, {showUpdatesCount} show updates.");
// Console.WriteLine($"{festival.ShowCount} shows, {festival.VenueCount} venues, {festival.PerformanceCount} performances.");

// var pos = new Position { Lat = 55.957, Lon = -3.17 }; // Example coordinates for Edinburgh


// var startDate = new DateTime(2026, 8, 12);
// var endDate = startDate.AddDays(1);
// var performancesComingUp = await festival.GetNearbyPerformancesAsync(pos, startDate, 
// 			endDate, 2); // Example: next 1 days within 2 km radius
// Console.WriteLine($"{performancesComingUp.Count,0} upcoming performances:");
// foreach (var performance in performancesComingUp)
// {
//     Console.WriteLine($"{performance.Start}: {performance.Show?.Title} at {performance.Venue?.Name}");
// }

// var venues = await festival.GetVenuesByLocationAsync(pos, 2); // Example: 2 km radius
// Console.WriteLine($"{venues.Count,0} nearby venues:");
// foreach (var venue in venues)
// {
//     Console.WriteLine($"{venue.Name} ({venue.Address})");
// }

// var venuesNearby = festival.GetVenuesByLocation(pos, 2); // Example: 2 km radius

// Console.WriteLine($"{venuesNearby.Count,0} statically nearby venues:");
// foreach (var venue in venuesNearby)
// {
//     Console.WriteLine($"{venue.Name} ({venue.Address})");
// }

// (venueUpdatesCount, showUpdatesCount) = await festival.UpdateFromFringeDataset();

// Console.WriteLine("Festival updated successfully.");
// Console.WriteLine($"{venueUpdatesCount} venue updates, {showUpdatesCount} show updates.");
// Console.WriteLine($"Now {festival.ShowCount} shows, {festival.VenueCount} venues, {festival.PerformanceCount} performances.");

/* var raw = File.CreateText("merged.json.gz");
var str = new GZipStream(raw.BaseStream, CompressionMode.Compress);
festival.SaveToStream(str);
str.Close();

var reloadedFestival = new Festival("5Jb4HXteE1tG8UWA", apiKey) { Name = EdinburghFestival.DemoFringe };
using var mergedStream = File.OpenRead("merged.json.gz");
using var gzipStream = new GZipStream(mergedStream, CompressionMode.Decompress);
reloadedFestival.LoadFromStream(gzipStream);
gzipStream.Close();
mergedStream.Close();

Console.WriteLine("Reloaded festival successfully.");
Console.WriteLine($"{reloadedFestival.VenueCount} venues, {reloadedFestival.ShowCount} shows, {reloadedFestival.PerformanceCount} performances.");

logStream.Close();
 */

// var logger = new Logger();

// var client = new ApiClient("5Jb4HXteE1tG8UWA", apiKey, "demofringe", logger);

// var shows = await client.GetDataAsync("events", "");

// var json = JsonDocument.Parse(shows);

// var f = File.CreateText("shows.json");
// f.Write(JsonSerializer.Serialize(json.RootElement, new JsonSerializerOptions { WriteIndented = true }));
// f.Close();

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
