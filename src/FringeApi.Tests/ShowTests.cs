using System;
using System.Text.Json;

namespace FringeApi.Tests;

// Includes Performance unit tests
public class ShowTests
{
    const string SampleShowJson = """
  {
    "id": "2c95ba44394448599f9fd5986589038bff7cfe36",
    "festival_id": "demofringe",
    "title": "FAKE On the Watchlist",
    "sub_title": null,
    "status": "active",
    "code": "DEMO:2026ONTHEWA",
    "year": 2026,
    "venue": {
      "id": "50058be44bcb8dba457ab29e53b8d4d0ab1af94a",
      "name": "DEMO: Mash Tonic The House at Just The",
      "map_ref": null,
      "code": "288",
      "description": "DEMO: a and installations. cultural comedy art Edinburgh exchange. becoming shows, performances, House Fringe, as of undergoes live theatrical gigs, and creativity a Mash Nestled space the The and emerges hub event city, pulsating dynamic area metamorphosis, the epicentre a experimental During this nightclub Cowgate vibrant music the converted in Festival of of",
      "address": "DEMO: St 37 Guthrie",
      "post_code": "EH1 1JG",
      "phone": "0330 220 1212",
      "fax": null,
      "email": null,
      "disabled_description": "DEMO: Assistance Cowgate reserved Limited areas. on in all joining of some parking, Street access stairway venue No No off and parking, toilet, on dogs parts just blue site, No wheelchair accessible accessible welcome to Guthrie Hidden street Cowgate the badge",
      "has_booking_over_phone": null,
      "has_booking_over_card": null,
      "has_booking_over_web": null,
      "box_office_opening": null,
      "box_office_fringe": null,
      "has_bar": null,
      "has_cafe": null,
      "cafe_description": null,
      "web_address": "http://www.justthetonic.com/edinburgh-festival/",
      "is_virtual": null,
      "position": {
        "lat": 55.948515,
        "lon": -3.187059
      }
    },
    "performances": [
      {
        "id": "186ys3w",
        "type": "in-person",
        "start": "2026-08-06 15:45:00",
        "end": "2026-08-06 16:45:00",
        "title": null,
        "price": 10,
        "price_type": "paid",
        "price_string": "\u00A310",
        "is_at_fixed_time": true,
        "duration_minutes": 60,
        "accessibility": [],
        "concession": null,
        "concession_family": null,
        "concession_additional": null
      },
      {
        "id": "23c948j",
        "type": "in-person",
        "start": "2026-08-07 15:45:00",
        "end": "2026-08-07 16:45:00",
        "title": null,
        "price": 10,
        "price_type": "paid",
        "price_string": "\u00A310",
        "is_at_fixed_time": true,
        "duration_minutes": 60,
        "accessibility": [],
        "concession": null,
        "concession_family": null,
        "concession_additional": null
      },
      {
        "id": "1rlgchj",
        "type": "in-person",
        "start": "2026-08-08 15:45:00",
        "end": "2026-08-08 16:45:00",
        "title": null,
        "price": 10,
        "price_type": "paid",
        "price_string": "\u00A310",
        "is_at_fixed_time": true,
        "duration_minutes": 60,
        "accessibility": [],
        "concession": null,
        "concession_family": null,
        "concession_additional": null
      },
      {
        "id": "1gunkqj",
        "type": "in-person",
        "start": "2026-08-09 15:45:00",
        "end": "2026-08-09 16:45:00",
        "title": null,
        "price": 10,
        "price_type": "paid",
        "price_string": "\u00A310",
        "is_at_fixed_time": true,
        "duration_minutes": 60,
        "accessibility": [],
        "concession": null,
        "concession_family": null,
        "concession_additional": null
      },
      {
        "id": "164uszj",
        "type": "in-person",
        "start": "2026-08-10 15:45:00",
        "end": "2026-08-10 16:45:00",
        "title": "2 for 1",
        "price": 10,
        "price_type": "paid",
        "price_string": "\u00A310",
        "is_at_fixed_time": true,
        "duration_minutes": 60,
        "accessibility": [],
        "concession": null,
        "concession_family": null,
        "concession_additional": null
      },
      {
        "id": "21a5556",
        "type": "in-person",
        "start": "2026-08-11 15:45:00",
        "end": "2026-08-11 16:45:00",
        "title": "2 for 1",
        "price": 10,
        "price_type": "paid",
        "price_string": "\u00A310",
        "is_at_fixed_time": true,
        "duration_minutes": 60,
        "accessibility": [],
        "concession": null,
        "concession_family": null,
        "concession_additional": null
      },
      {
        "id": "1pjcde6",
        "type": "in-person",
        "start": "2026-08-12 15:45:00",
        "end": "2026-08-12 16:45:00",
        "title": null,
        "price": 10,
        "price_type": "paid",
        "price_string": "\u00A310",
        "is_at_fixed_time": true,
        "duration_minutes": 60,
        "accessibility": [],
        "concession": null,
        "concession_family": null,
        "concession_additional": null
      },
      {
        "id": "1esjln6",
        "type": "in-person",
        "start": "2026-08-13 15:45:00",
        "end": "2026-08-13 16:45:00",
        "title": null,
        "price": 10,
        "price_type": "paid",
        "price_string": "\u00A310",
        "is_at_fixed_time": true,
        "duration_minutes": 60,
        "accessibility": [],
        "concession": null,
        "concession_family": null,
        "concession_additional": null
      },
      {
        "id": "142qtw6",
        "type": "in-person",
        "start": "2026-08-14 15:45:00",
        "end": "2026-08-14 16:45:00",
        "title": null,
        "price": 10,
        "price_type": "paid",
        "price_string": "\u00A310",
        "is_at_fixed_time": true,
        "duration_minutes": 60,
        "accessibility": [],
        "concession": null,
        "concession_family": null,
        "concession_additional": null
      },
      {
        "id": "1y8161s",
        "type": "in-person",
        "start": "2026-08-15 15:45:00",
        "end": "2026-08-15 16:45:00",
        "title": null,
        "price": 10,
        "price_type": "paid",
        "price_string": "\u00A310",
        "is_at_fixed_time": true,
        "duration_minutes": 60,
        "accessibility": [],
        "concession": null,
        "concession_family": null,
        "concession_additional": null
      },
      {
        "id": "1nh8eas",
        "type": "in-person",
        "start": "2026-08-16 15:45:00",
        "end": "2026-08-16 16:45:00",
        "title": null,
        "price": 10,
        "price_type": "paid",
        "price_string": "\u00A310",
        "is_at_fixed_time": true,
        "duration_minutes": 60,
        "accessibility": [],
        "concession": null,
        "concession_family": null,
        "concession_additional": null
      },
      {
        "id": "1cqfmjs",
        "type": "in-person",
        "start": "2026-08-17 15:45:00",
        "end": "2026-08-17 16:45:00",
        "title": null,
        "price": 10,
        "price_type": "paid",
        "price_string": "\u00A310",
        "is_at_fixed_time": true,
        "duration_minutes": 60,
        "accessibility": [],
        "concession": null,
        "concession_family": null,
        "concession_additional": null
      }
    ],
    "performance_space": {
      "name": "DEMO: Just Room Cask The",
      "capacity": 80,
      "wheelchair_access": false,
      "age_limited": false,
      "age_limit": null
    },
    "sub_venue": null,
    "warnings": "Contains distressing or potentially triggering themes, Strong language/swearing",
    "fringe_first": null,
    "description": "DEMO: incisive dives the segments its Rooted the weird, and the late-night On and autopsy manipulation Join comedy with a real our that is critics. voice, live cultural satire portraying features political Watchlist with with Each Servedio Jay\u0027s into exorcism. headfirst in silly headlines, investigative show, field media live own absurdist into sketches, the conversations and blends On show unapologetically Watchlist and for is monologue, media with week\u0027s us curiosity. series of Watchlist Jay Age plus On the and Misinformation. chaotic, biting Smart, exploration brand-new parts social figures: public journalists, comedy theatre. comedians character interviews writers equal",
    "description_teaser": null,
    "artist": "DEMO: Jay Servedio",
    "artist_type": "Semi-professional company",
    "genre": "Comedy",
    "genre_tags": "Alternative Comedy,Absurdist",
    "categories": {
      "strand_titles": [],
      "subjects": [],
      "keywords": []
    },
    "discounts": {
      "two_for_one": null,
      "group": null,
      "friends": null,
      "passport": null,
      "schools": null
    },
    "disabled": {
      "audio": null,
      "audio_dates": null,
      "signed": null,
      "signed_dates": null,
      "captioning": null,
      "captioning_dates": null,
      "other_services": null,
      "other_services_dates": null,
      "other_services_information": null
    },
    "age_category": "18\u002B",
    "website": "https://www.edfringe.com/tickets/whats-on/2026ONTHEWA",
    "images": {
      "16af76729395ab28ec514de6c3e1a291": {
        "hash": "16af76729395ab28ec514de6c3e1a291",
        "type": "thumb",
        "orientation": "square",
        "versions": {
          "original": {
            "type": "original",
            "width": 250,
            "height": 250,
            "mime": "image/jpeg",
            "url": "https://images.api.edinburghfestivalcity.com/F-181/16af76729395ab28ec514de6c3e1a291-original"
          },
          "square-75": {
            "type": "square-75",
            "width": 75,
            "height": 75,
            "mime": "image/jpeg",
            "url": "https://images.api.edinburghfestivalcity.com/F-181/16af76729395ab28ec514de6c3e1a291-square-75.jpg"
          },
          "thumb-100": {
            "type": "thumb-100",
            "width": 100,
            "height": 100,
            "mime": "image/jpeg",
            "url": "https://images.api.edinburghfestivalcity.com/F-181/16af76729395ab28ec514de6c3e1a291-thumb-100.jpg"
          },
          "square-150": {
            "type": "square-150",
            "width": 150,
            "height": 150,
            "mime": "image/jpeg",
            "url": "https://images.api.edinburghfestivalcity.com/F-181/16af76729395ab28ec514de6c3e1a291-square-150.jpg"
          }
        }
      }
    },
    "twitter": null,
    "country": "UNITED STATES",
    "performers_number": 6,
    "non_english": null,
    "related_content": [
      {
        "url": "https://www.youtube.com/@OnTheWatchlistWithJayServedio"
      }
    ],
    "festival": "DEMO Fringe data for testing",
    "url": "https://api.edinburghfestivalcity.com/events/2c95ba44394448599f9fd5986589038bff7cfe36",
    "updated": "2026-04-03 11:04:24",
    "update_times": {
      "identity": 1775210664,
      "title": 1775210664,
      "description": 1775210664,
      "venue": 1775210664,
      "performances": 1775210664,
      "artist": 1775210664,
      "genre": 1775210664,
      "discounts": 1775210664,
      "age_category": 1775210664,
      "fringe_first": 1775210664,
      "other": 1775210664,
      "disabled": 1775210664,
      "web_links": 1775210664,
      "images": 1775210664,
      "status": 1775210664
    },
    "latitude": 55.948515,
    "longitude": -3.187059
  }
""";

    [Fact]
    public void ComplexShowWithPerformances()
    {
        var festival = new Festival("id", "key");
        var show = new Show() { Festival = festival, Id = "2c95ba44394448599f9fd5986589038bff7cfe36" };
        show.UpdateFromJson(JsonDocument.Parse(SampleShowJson).RootElement);

        Assert.Equal("2c95ba44394448599f9fd5986589038bff7cfe36", show.Id);
        Assert.Equal(ShowStatus.Active, show.Status);
        Assert.Equal("FAKE On the Watchlist", show.Title);
        Assert.Equal(12, show.Performances.Count);
        Assert.Equal(12, show.PerformancesById.Count);
        Assert.All(show.Performances, p => Assert.Equal(show, p.Show));
        Assert.Contains("186ys3w", show.PerformancesById);
    }
}
