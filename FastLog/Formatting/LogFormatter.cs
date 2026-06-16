using FastLog.Models;

namespace FastLog.Formatting
{
    /// <summary>
    /// Formats one log message into the final text written by sinks.
    /// </summary>
    /// <param name="message">Log message to format.</param>
    /// <returns>Formatted log line.</returns>
    public delegate string LogFormatter(LogMessage message);
}
