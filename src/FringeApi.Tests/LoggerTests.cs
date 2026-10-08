namespace FringeApi.Tests;

public class LoggerTests
{
    [Fact]
    public void LogMessageTest()
    {
        const string fileName = "test-log.txt";
        var logger = new Logger();
        var file = File.CreateText(fileName);
        logger.SetLogStream(file);
        var message = "Test message";
        logger.Log(message);
        file.Close();
        var fileContents = File.ReadAllText(fileName);
        Assert.Contains(message, fileContents);
        File.Delete(fileName);
    }
}
