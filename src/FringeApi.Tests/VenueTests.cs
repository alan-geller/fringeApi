using System;
using System.Text.Json;

namespace FringeApi.Tests;

public class VenueTests
{
    const string TestVenue1Json = "{ \"name\": \"Test Venue\", \"id\": \"1\" }";
    const string TestVenue2Json = """
{
"name": "DEMO: Farmhouse Point Meeting Bridgend at",
"festival": "DEMO Fringe data for testing",
"festival_id": "demofringe",
"year": 2026,
"code": "238",
"website": null,
"address": "DEMO: Old Dalkeith Road 41",
"post_code": "EH16 4TE",
"position": {
    "lat": 55.926914,
    "lon": -3.153717
},
"phone": null,
"email": null,
"description": "DEMO: point. Meeting",
"access_description": "DEMO: wheelchair accessible toilet No",
"performance_spaces": [
    {
    "name": "DEMO: Point Meeting",
    "capacity": 10,
    "wheelchair_access": false,
    "age_limited": false,
    "age_limit": null
    }
],
"event_count": 1,
"performance_count": 22,
"status": "in-use",
"updated": "2026-02-11 17:32:03",
"is_virtual": null,
"id": "510b67b924c227be10a0d00293f43e588a3bf528"
}
""";

    [Fact]
    public void TestSimpleVenueJson()
    {
        var festival = new Festival("id", "key", EdinburghFestival.DemoFringe);
        var venue = new Venue { Festival = festival, Id = "1" };
        venue.UpdateFromJson(JsonDocument.Parse(TestVenue1Json).RootElement);
        Assert.Equal("Test Venue", venue.Name);
        Assert.Equal("1", venue.Id);
    }
    
    [Fact]
    public void TestComplexVenueJson()
    {
        var festival = new Festival("id", "key", EdinburghFestival.DemoFringe);
        var venue = new Venue { Festival = festival, Id = "510b67b924c227be10a0d00293f43e588a3bf528" };
        venue.UpdateFromJson(JsonDocument.Parse(TestVenue2Json).RootElement);
        Assert.Equal("DEMO: Farmhouse Point Meeting Bridgend at", venue.Name);
        Assert.Equal("510b67b924c227be10a0d00293f43e588a3bf528", venue.Id);
        Assert.Equal("DEMO: Old Dalkeith Road 41", venue.Address);
        Assert.Equal("238", venue.Code);
        Assert.Equal(new DateTime(2026, 2, 11, 17, 32, 03), venue.LastUpdated);
        Assert.NotNull(venue.Position);
        Assert.Equal(55.926914, venue.Position!.Lat);
        Assert.Equal(-3.153717, venue.Position.Lon);
        var extra = venue.Extra;
        Assert.NotNull(extra);
        Assert.Equal("DEMO Fringe data for testing", extra!["festival"].GetString());
        Assert.Equal("demofringe", extra!["festival_id"].GetString());
        Assert.Equal(2026, extra!["year"].GetInt32());
        Assert.Equal("DEMO: point. Meeting", extra!["description"].GetString());
        Assert.Equal("DEMO: wheelchair accessible toilet No", extra!["access_description"].GetString());
    }
}
