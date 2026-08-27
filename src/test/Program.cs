using FringeApi;

var venue = new Venue
{
    Name = "The Venue",
    Address = "1 Festival Street, Edinburgh",
    Code = "TV",
    LastUpdated = new DateTime(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc),
    Extra = new Dictionary<string, string> { ["capacity"] = "120" }
};

var show = new Show
{
    Title = "Example Show",
    Genre = "Comedy",
    Performer = "Demo Comic",
    Description = "A test show.",
    Venue = venue,
    LastUpdated = new DateTime(2026, 8, 1, 9, 30, 0, DateTimeKind.Utc),
    Extra = new Dictionary<string, string> { ["tag"] = "preview" }
};

var performance = new Performance
{
    Show = show,
    Venue = venue,
    Date = new DateTime(2026, 8, 5, 0, 0, 0, DateTimeKind.Utc),
    Time = new TimeSpan(19, 30, 0),
    LastUpdated = new DateTime(2026, 8, 1, 11, 0, 0, DateTimeKind.Utc),
    Extra = new Dictionary<string, string> { ["status"] = "booked" }
};

show.Performances.Add(performance);
venue.Performances.Add(performance);

var festival = new Festival
{
    Name = "Edinburgh Fringe",
    Location = "Edinburgh",
    LastUpdated = new DateTime(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc),
    Extra = new Dictionary<string, string> { ["city"] = "Edinburgh" }
};

festival.UpdateFromFringeDataset(
    new[] { show },
    new[] { performance },
    new[] { venue });

var showsInRange = festival.GetShowsByDate(
    new DateTime(2026, 8, 5, 0, 0, 0, DateTimeKind.Utc),
    new DateTime(2026, 8, 6, 0, 0, 0, DateTimeKind.Utc),
    genre: "Comedy",
    venueName: "The Venue");

if (showsInRange.Count != 1)
{
    throw new InvalidOperationException($"Expected 1 show in range, got {showsInRange.Count}.");
}

var nearby = festival.GetNearbyPerformances(
    55.95,
    -3.18,
    new DateTime(2026, 8, 5, 18, 0, 0, DateTimeKind.Utc),
    new DateTime(2026, 8, 5, 23, 0, 0, DateTimeKind.Utc),
    maxDistanceKm: 25,
    maxLeadTime: TimeSpan.FromHours(6));

if (nearby.Count != 1)
{
    throw new InvalidOperationException($"Expected 1 nearby performance, got {nearby.Count}.");
}

Console.WriteLine("Festival design regression checks passed.");
