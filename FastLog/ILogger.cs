using System;
using FastLog.Models;
using FastLog.Sinks;

namespace FastLog
{
    /// <summary>
    /// Defines the logging contract used by application and business layers.
    /// </summary>
    public interface ILogger : IDisposable
    {
        /// <summary>
        /// Gets or sets the minimum log level that can be written.
        /// </summary>
        LogLevel MinimumLevel { get; set; }

        /// <summary>
        /// Writes one log message with caller metadata.
        /// </summary>
        /// <param name="level">Severity of the log message.</param>
        /// <param name="message">Log message content.</param>
        /// <param name="memberName">Caller member name supplied by the compiler.</param>
        /// <param name="filePath">Caller source file path supplied by the compiler.</param>
        /// <param name="lineNumber">Caller source line number supplied by the compiler.</param>
        void Log(LogLevel level, string message, string memberName, string filePath, int lineNumber);

        /// <summary>
        /// Writes exception details with caller metadata.
        /// </summary>
        /// <param name="level">Severity of the exception log.</param>
        /// <param name="exception">Exception to record.</param>
        /// <param name="message">Optional operation description.</param>
        /// <param name="memberName">Caller member name supplied by the compiler.</param>
        /// <param name="filePath">Caller source file path supplied by the compiler.</param>
        /// <param name="lineNumber">Caller source line number supplied by the compiler.</param>
        void LogException(LogLevel level, Exception exception, string message, string memberName, string filePath, int lineNumber);

        /// <summary>
        /// Changes the minimum log level at runtime.
        /// </summary>
        /// <param name="level">New minimum log level.</param>
        void SetLevel(LogLevel level);

        /// <summary>
        /// Changes the built-in log output target at runtime.
        /// </summary>
        /// <param name="sinkType">Built-in sink type.</param>
        void SetSink(LogSinkType sinkType);

        /// <summary>
        /// Changes the log output target to a custom sink at runtime.
        /// </summary>
        /// <param name="sink">Custom sink implementation.</param>
        void SetSink(ILogSink sink);

        /// <summary>
        /// Gets current logger runtime counters.
        /// </summary>
        /// <returns>Runtime statistics snapshot.</returns>
        LoggerStatisticsSnapshot GetStatistics();

        void Flush();
    }
}
