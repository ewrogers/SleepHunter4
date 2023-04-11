using System;
using System.Runtime.CompilerServices;

namespace SleepHunter.Services.Logging
{
    public interface ILogger : IDisposable
    {
        bool AutoFlush { get; set; }

        void LogInfo(string message, string category = "");
        void LogWarn(string message, string category = "");
        void LogError(string message, string category = "");

        void LogException(Exception exception, string category = "", [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 1);
        void LogDebug(string message, string category = "", [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 1);

        void AddFileTransport(string filePath);
    }
}
