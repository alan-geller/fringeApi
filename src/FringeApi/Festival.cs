using System.Text.Json;

namespace FringeApi;

public class Festival
{
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public Dictionary<string, string> Extra { get; set; } = new();

    public List<Show> Shows { get; } = new();
    public List<Performance> Performances { get; } = new();
    public List<Venue> Venues { get; } = new();

    public void UpdateFromFringeDataset(IEnumerable<Show> shows, IEnumerable<Performance> performances, IEnumerable<Venue> venues)
    {
        Shows.Clear();
        Performances.Clear();
        Venues.Clear();

        foreach (var venue in venues)
        {
            Venues.Add(venue);
        }

        foreach (var show in shows)
        {
            Shows.Add(show);
            foreach (var performance in show.Performances)
            {
                if (!Performances.Contains(performance))
                {
                    Performances.Add(performance);
                }
                if (performance.Venue != null && !performance.Venue.Performances.Contains(performance))
                {
                    performance.Venue.Performances.Add(performance);
                }
            }
        }

        foreach (var performance in performances)
        {
            if (!Performances.Contains(performance))
            {
                Performances.Add(performance);
            }

            if (performance.Show != null && !performance.Show.Performances.Contains(performance))
            {
                performance.Show.Performances.Add(performance);
            }

            if (performance.Venue != null && !performance.Venue.Performances.Contains(performance))
            {
                performance.Venue.Performances.Add(performance);
            }
        }

        foreach (var show in Shows)
        {
            if (show.Venue != null && !Venues.Contains(show.Venue))
            {
                Venues.Add(show.Venue);
            }
        }
    }

    public List<Show> GetShowsByDate(DateTime start, DateTime end, string? genre = null, string? venueName = null)
    {
        if (end < start)
        {
            (start, end) = (end, start);
        }

        return Shows
            .Where(show => show.Performances.Any(performance => performance.Date >= start && performance.Date <= end))
            .Where(show => string.IsNullOrWhiteSpace(genre) || string.Equals(show.Genre, genre, StringComparison.OrdinalIgnoreCase))
            .Where(show => string.IsNullOrWhiteSpace(venueName) || string.Equals(show.Venue?.Name, venueName, StringComparison.OrdinalIgnoreCase) || show.Performances.Any(p => string.Equals(p.Venue?.Name, venueName, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(show => show.Title)
            .ToList();
    }

    public List<Performance> GetNearbyPerformances(double latitude, double longitude, DateTime start, DateTime end, double maxDistanceKm, TimeSpan maxLeadTime)
    {
        if (end < start)
        {
            (start, end) = (end, start);
        }

        return Performances
            .Where(p => p.Start >= start && p.Start <= end)
            .Where(p => p.Venue != null)
            .Where(p =>
            {
                if (p.Venue is null) return false;
                if (string.IsNullOrWhiteSpace(p.Venue.Code) && string.IsNullOrWhiteSpace(p.Venue.Address))
                {
                    return true;
                }

                return true;
            })
            .OrderBy(p => p.Start)
            .ToList();
    }

    public static Festival FromJson(string json)
    {
        return JsonSerializer.Deserialize<Festival>(json) ?? new Festival();
    }
}
