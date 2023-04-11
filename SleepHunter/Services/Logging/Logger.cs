using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace SleepHunter.Services.Logging
{
    public class Logger : ILogger
    {
        private bool isDisposed;
        private bool autoFlush = true;

        public bool AutoFlush
        {
            get => autoFlush;
            set
            {
                autoFlush = value;
                Trace.AutoFlush = value;
                Debug.AutoFlush = value;
            }
        }

        public Logger() { }

        private readonly List<TextWriter> transports = new List<TextWriter>();

        ~Logger() => Dispose(false);

        public void LogInfo(string message)
        {
            CheckIfDisposed();
            Log(message, "info");
        }

        public void LogWarn(string message)
        {
            Log(message, "warn");
        }

        public void LogError(string message)
        {
            Log(message, "error");
        }

        public void LogException(Exception ex, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 1)
        {
            CheckIfDisposed();

            var exceptionName = ex.GetType().Name;
            var fileName = Path.GetFileName(filePath);

            var messageWithTrace = $"{exceptionName} thrown in {fileName}:{lineNumber} ({memberName}): {ex.Message}\nStack Trace:\n{ex.StackTrace}";
            Log(messageWithTrace, "exception");
        }

        public void LogDebug(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 1)
        {
            CheckIfDisposed();

            var fileName = Path.GetFileName(filePath);
            var messageWithContext = $"{fileName}:{lineNumber} ({memberName}): {message}";

            Log(messageWithContext, "debug");
        }

        public void AddFileTransport(string filePath)
        {
            CheckIfDisposed();

            var directory = Path.GetDirectoryName(filePath);

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var stream = File.OpenWrite(filePath);
            var writer = new StreamWriter(stream, Encoding.UTF8);

            transports.Add(writer);
        }

        private void Log(string message, string level = "info")
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var formattedLine = $"{timestamp} {level.ToUpperInvariant()}: {message}";

            if (level != "debug")
                Trace.WriteLine(formattedLine);
            else
                Debug.WriteLine(formattedLine);

            foreach (var writer in transports)
            {
                writer.WriteLine(formattedLine);

                if (AutoFlush)
                    writer.Flush();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool isDisposing)
        {
            if (isDisposed) return;

            if (isDisposing)
            {
                foreach (var writer in transports)
                {
                    writer.Flush();
                    writer.Dispose();
                }
            }

            isDisposed = true;
        }

        private void CheckIfDisposed()
        {
            if (isDisposed)
                throw new ObjectDisposedException(GetType().Name);
        }
    }
}
