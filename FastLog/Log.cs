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

        public static void Initialize(Action<LoggerConfiguration> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            LoggerConfiguration configuration = new LoggerConfiguration();
            configure(configuration);
            SetLogger(configuration.CreateLogger());
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

        public static void Trace(string format, params object[] args)
        {
            Current.LogFormat(LogLevel.Trace, format, args, string.Empty, string.Empty, 0);
        }
        public static void Trace<T1>(string format, T1 arg1, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Current.LogFormat(LogLevel.Trace, format, new object[] { arg1 }, memberName, filePath, lineNumber);
        }

        public static void Trace<T1, T2>(string format, T1 arg1, T2 arg2, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Current.LogFormat(LogLevel.Trace, format, new object[] { arg1, arg2 }, memberName, filePath, lineNumber);
        }

        public static void Trace<T1, T2, T3>(string format, T1 arg1, T2 arg2, T3 arg3, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Current.LogFormat(LogLevel.Trace, format, new object[] { arg1, arg2, arg3 }, memberName, filePath, lineNumber);
        }

        public static void Trace<T1, T2, T3, T4>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Current.LogFormat(LogLevel.Trace, format, new object[] { arg1, arg2, arg3, arg4 }, memberName, filePath, lineNumber);
        }
        public static void Debug(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Current.Log(LogLevel.Debug, message, memberName, filePath, lineNumber);
        }

        public static void Debug(string format, params object[] args)
        {
            Current.LogFormat(LogLevel.Debug, format, args, string.Empty, string.Empty, 0);
        }
        public static void Debug<T1>(string format, T1 arg1, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Current.LogFormat(LogLevel.Debug, format, new object[] { arg1 }, memberName, filePath, lineNumber);
        }

        public static void Debug<T1, T2>(string format, T1 arg1, T2 arg2, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Current.LogFormat(LogLevel.Debug, format, new object[] { arg1, arg2 }, memberName, filePath, lineNumber);
        }

        public static void Debug<T1, T2, T3>(string format, T1 arg1, T2 arg2, T3 arg3, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Current.LogFormat(LogLevel.Debug, format, new object[] { arg1, arg2, arg3 }, memberName, filePath, lineNumber);
        }

        public static void Debug<T1, T2, T3, T4>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Current.LogFormat(LogLevel.Debug, format, new object[] { arg1, arg2, arg3, arg4 }, memberName, filePath, lineNumber);
        }
        public static void Info(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Current.Log(LogLevel.Info, message, memberName, filePath, lineNumber);
        }

        public static void Info(string format, params object[] args)
        {
            Current.LogFormat(LogLevel.Info, format, args, string.Empty, string.Empty, 0);
        }
        public static void Info<T1>(string format, T1 arg1, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Current.LogFormat(LogLevel.Info, format, new object[] { arg1 }, memberName, filePath, lineNumber);
        }

        public static void Info<T1, T2>(string format, T1 arg1, T2 arg2, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Current.LogFormat(LogLevel.Info, format, new object[] { arg1, arg2 }, memberName, filePath, lineNumber);
        }

        public static void Info<T1, T2, T3>(string format, T1 arg1, T2 arg2, T3 arg3, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Current.LogFormat(LogLevel.Info, format, new object[] { arg1, arg2, arg3 }, memberName, filePath, lineNumber);
        }

        public static void Info<T1, T2, T3, T4>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Current.LogFormat(LogLevel.Info, format, new object[] { arg1, arg2, arg3, arg4 }, memberName, filePath, lineNumber);
        }
        public static void Warn(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Current.Log(LogLevel.Warn, message, memberName, filePath, lineNumber);
        }

        public static void Warn(string format, params object[] args)
        {
            Current.LogFormat(LogLevel.Warn, format, args, string.Empty, string.Empty, 0);
        }
        public static void Warn<T1>(string format, T1 arg1, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Current.LogFormat(LogLevel.Warn, format, new object[] { arg1 }, memberName, filePath, lineNumber);
        }

        public static void Warn<T1, T2>(string format, T1 arg1, T2 arg2, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Current.LogFormat(LogLevel.Warn, format, new object[] { arg1, arg2 }, memberName, filePath, lineNumber);
        }

        public static void Warn<T1, T2, T3>(string format, T1 arg1, T2 arg2, T3 arg3, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Current.LogFormat(LogLevel.Warn, format, new object[] { arg1, arg2, arg3 }, memberName, filePath, lineNumber);
        }

        public static void Warn<T1, T2, T3, T4>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Current.LogFormat(LogLevel.Warn, format, new object[] { arg1, arg2, arg3, arg4 }, memberName, filePath, lineNumber);
        }
        public static void Error(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Current.Log(LogLevel.Error, message, memberName, filePath, lineNumber);
        }

        public static void Error(string format, params object[] args)
        {
            Current.LogFormat(LogLevel.Error, format, args, string.Empty, string.Empty, 0);
        }
        public static void Error<T1>(string format, T1 arg1, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Current.LogFormat(LogLevel.Error, format, new object[] { arg1 }, memberName, filePath, lineNumber);
        }

        public static void Error<T1, T2>(string format, T1 arg1, T2 arg2, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Current.LogFormat(LogLevel.Error, format, new object[] { arg1, arg2 }, memberName, filePath, lineNumber);
        }

        public static void Error<T1, T2, T3>(string format, T1 arg1, T2 arg2, T3 arg3, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Current.LogFormat(LogLevel.Error, format, new object[] { arg1, arg2, arg3 }, memberName, filePath, lineNumber);
        }

        public static void Error<T1, T2, T3, T4>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Current.LogFormat(LogLevel.Error, format, new object[] { arg1, arg2, arg3, arg4 }, memberName, filePath, lineNumber);
        }
        public static void Error(Exception exception, string message = "", [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Current.LogException(LogLevel.Error, exception, message, memberName, filePath, lineNumber);
        }

        public static void Fatal(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Current.Log(LogLevel.Fatal, message, memberName, filePath, lineNumber);
        }

        public static void Fatal(string format, params object[] args)
        {
            Current.LogFormat(LogLevel.Fatal, format, args, string.Empty, string.Empty, 0);
        }
        public static void Fatal<T1>(string format, T1 arg1, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Current.LogFormat(LogLevel.Fatal, format, new object[] { arg1 }, memberName, filePath, lineNumber);
        }

        public static void Fatal<T1, T2>(string format, T1 arg1, T2 arg2, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Current.LogFormat(LogLevel.Fatal, format, new object[] { arg1, arg2 }, memberName, filePath, lineNumber);
        }

        public static void Fatal<T1, T2, T3>(string format, T1 arg1, T2 arg2, T3 arg3, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Current.LogFormat(LogLevel.Fatal, format, new object[] { arg1, arg2, arg3 }, memberName, filePath, lineNumber);
        }

        public static void Fatal<T1, T2, T3, T4>(string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Current.LogFormat(LogLevel.Fatal, format, new object[] { arg1, arg2, arg3, arg4 }, memberName, filePath, lineNumber);
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



