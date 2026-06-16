using System.Globalization;
using FastLog.Formatting;
using FastLog.Models;

namespace FastLog.Sinks
{
    public abstract class LogSinkBase : ILogSink
    {
        public abstract void Write(LogMessage message);

        public virtual void Flush()
        {
        }

        public virtual void Dispose()
        {
        }

        protected static string FormatFull(LogMessage log)
        {
            return FormatFull(log, null);
        }

        protected static string FormatFull(LogMessage log, LogFormatter formatter)
        {
            if (formatter != null)
            {
                return formatter(log);
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                "{0:yyyy-MM-dd HH:mm:ss.fff} [{1}] [{2}] [TID:{3}] {4}() {5} : {6}",
                log.Timestamp.LocalDateTime,
                ToLevelText(log.Level),
                log.FileName,
                log.ThreadId,
                log.MemberName,
                log.LineNumber,
                log.Message);
        }

        protected static string FormatShort(LogMessage log)
        {
            return FormatShort(log, null);
        }

        protected static string FormatShort(LogMessage log, LogFormatter formatter)
        {
            if (formatter != null)
            {
                return formatter(log);
            }

            return string.Format(
                CultureInfo.InvariantCulture,
                "{0:HH:mm:ss.fff} [{1}] [{2}] [TID:{3}] {4}() {5} : {6}",
                log.Timestamp.LocalDateTime,
                ToLevelText(log.Level),
                log.FileName,
                log.ThreadId,
                log.MemberName,
                log.LineNumber,
                log.Message);
        }

        public static string ToLevelText(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Trace:
                    return "TRACE";
                case LogLevel.Debug:
                    return "DEBUG";
                case LogLevel.Info:
                    return "INFO";
                case LogLevel.Warn:
                    return "WARN";
                case LogLevel.Error:
                    return "ERROR";
                case LogLevel.Fatal:
                    return "FATAL";
                default:
                    return "UNKNOWN";
            }
        }
    }
}
