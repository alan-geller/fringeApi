namespace FringeApi.Tests;

public class LocationTests
{
    [Fact]
    public void SamePositionTest()
    {
        var pos1 = new Position { Lat = 55.957, Lon = -3.17 };
        var pos2 = new Position { Lat = 55.957, Lon = -3.17 };

        var distance = pos1.DistanceTo(pos2);
        Assert.Equal(0.0, distance, 10);
    }

    [Fact]
    public void PoleToEquatorDistanceTest()
    {
        var pos1 = new Position { Lat = 0, Lon = 0 };
        var pos2 = new Position { Lat = 90, Lon = 0 };

        var distance = pos1.DistanceTo(pos2);
        Assert.Equal(10000.0, distance, 100.0);
    }

    [Fact]
    public void AroundTTheEquatorDistanceTest()
    {
        var pos1 = new Position { Lat = 0, Lon = 0 };
        var pos2 = new Position { Lat = 0, Lon = 90 };

        var distance = pos1.DistanceTo(pos2);
        Assert.Equal(10000.0, distance, 100.0);
    }
}
