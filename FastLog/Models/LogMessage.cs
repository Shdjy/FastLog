using System;

namespace FastLog.Models
{
    /// <summary>
    /// Represents a single immutable log entry.
    /// </summary>
    public sealed class LogMessage
    {
        public LogMessage(
            LogLevel level,
            DateTimeOffset timestamp,
            int threadId,
            string fileName,
            string memberName,
            int lineNumber,
            string message)
        {
            Level = level;
            Timestamp = timestamp;
            ThreadId = threadId;
            FileName = fileName ?? string.Empty;
            MemberName = memberName ?? string.Empty;
            LineNumber = lineNumber;
            Message = message ?? string.Empty;
        }

        public LogLevel Level { get; }

        public DateTimeOffset Timestamp { get; }

        public int ThreadId { get; }

        public string FileName { get; }

        public string MemberName { get; }

        public int LineNumber { get; }

        public string Message { get; }
    }
}



