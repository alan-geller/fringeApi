internal class Logger
{
    private StreamWriter? logStream;

    public void SetLogStream(StreamWriter logStream)
    {
        if (this.logStream != null)
        {
            this.logStream.Dispose();
        }
        this.logStream = logStream;
    }

    internal void Log(string message)
    {
        if (logStream != null)
        {
            logStream.WriteLine(message);
            logStream.Flush();
        }
    }

    internal void LogException(Exception ex, string context)
    {
        Log($"Exception {ex.Message} while '{context}'");
    }
}