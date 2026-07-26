using SleepHunter.Services.Logging;

namespace SleepHunter.Tests.Support;

internal sealed class TestLogger : ILogger
{
    public bool AutoFlush { get; set; }

    public List<string> Errors { get; } = [];

    public List<string> Information { get; } = [];

    public List<string> Warnings { get; } = [];

    public List<Exception> Exceptions { get; } = [];

    public void AddFileTransport(string filePath)
    {
    }

    public void Dispose()
    {
    }

    public void LogDebug(
        string message,
        string category = "",
        string memberName = "",
        string filePath = "",
        int lineNumber = 1)
    {
    }

    public void LogError(
        string message,
        string category = "") =>
        Errors.Add(message);

    public void LogException(
        Exception exception,
        string category = "",
        string memberName = "",
        string filePath = "",
        int lineNumber = 1) =>
        Exceptions.Add(exception);

    public void LogInfo(
        string message,
        string category = "") =>
        Information.Add(message);

    public void LogWarn(
        string message,
        string category = "") =>
        Warnings.Add(message);
}
