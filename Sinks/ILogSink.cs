using System;
using FastLog.Models;

namespace FastLog.Sinks
{
    /// <summary>
    /// Writes formatted log messages to a concrete target.
    /// </summary>
    public interface ILogSink : IDisposable
    {
        /// <summary>
        /// Writes one log message.
        /// </summary>
        /// <param name="message">The log message to write.</param>
        void Write(LogMessage message);

        /// <summary>
        /// Flushes buffered log data to the target.
        /// </summary>
        void Flush();
    }
}



