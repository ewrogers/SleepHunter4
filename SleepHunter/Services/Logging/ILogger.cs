using System;
using System.Runtime.CompilerServices;

namespace SleepHunter.Services.Logging
{
    public interface ILogger : IDisposable
    {
        bool AutoFlush { get; set; }

        void LogInfo(string message);
        void LogWarn(string message);
        void LogError(string message);

        void LogException(Exception exception, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 1);
        void LogDebug(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 1);

        void AddFileTransport(string filePath);
    }
}
