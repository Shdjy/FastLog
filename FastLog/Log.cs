using System;
using System.Runtime.CompilerServices;
using FastLog.Configuration;
using FastLog.Models;
using FastLog.Sinks;

namespace FastLog
{
    public static class Log
    {
        private static readonly object SyncRoot = new object();
        private static ILogger _logger;

        public static ILogger Current
        {
            get
            {
                EnsureInitialized();
                return _logger;
            }
        }

        public static void Initialize(LoggerOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            lock (SyncRoot)
            {
                ILogger oldLogger = _logger;
                _logger = new Logger(options);

                if (oldLogger != null)
                {
                    oldLogger.Dispose();
                }
            }
        }

        public static void SetLogger(ILogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            lock (SyncRoot)
            {
                ILogger oldLogger = _logger;
                _logger = logger;

                if (oldLogger != null)
                {
                    oldLogger.Dispose();
                }
            }
        }

        public static void SetLevel(LogLevel level)
        {
            Current.SetLevel(level);
        }

        public static void SetSink(LogSinkType sinkType)
        {
            Current.SetSink(sinkType);
        }

        public static void SetSink(ILogSink sink)
        {
            Current.SetSink(sink);
        }

        public static LoggerStatisticsSnapshot GetStatistics()
        {
            return Current.GetStatistics();
        }

        public static void Flush()
        {
            Current.Flush();
        }

        public static void Trace(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Current.Log(LogLevel.Trace, message, memberName, filePath, lineNumber);
        }

        public static void Debug(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Current.Log(LogLevel.Debug, message, memberName, filePath, lineNumber);
        }

        public static void Info(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Current.Log(LogLevel.Info, message, memberName, filePath, lineNumber);
        }

        public static void Warn(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Current.Log(LogLevel.Warn, message, memberName, filePath, lineNumber);
        }

        public static void Error(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Current.Log(LogLevel.Error, message, memberName, filePath, lineNumber);
        }

        public static void Error(Exception exception, string message = "", [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Current.LogException(LogLevel.Error, exception, message, memberName, filePath, lineNumber);
        }

        public static void Fatal(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Current.Log(LogLevel.Fatal, message, memberName, filePath, lineNumber);
        }

        public static void Fatal(Exception exception, string message = "", [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Current.LogException(LogLevel.Fatal, exception, message, memberName, filePath, lineNumber);
        }

        public static void Shutdown()
        {
            lock (SyncRoot)
            {
                if (_logger != null)
                {
                    _logger.Dispose();
                    _logger = null;
                }
            }
        }

        private static void EnsureInitialized()
        {
            if (_logger != null)
            {
                return;
            }

            lock (SyncRoot)
            {
                if (_logger == null)
                {
                    _logger = new Logger(new LoggerOptions());
                }
            }
        }
    }
}
