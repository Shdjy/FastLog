using System.Diagnostics;
using FastLog.Formatting;
using FastLog.Models;

namespace FastLog.Sinks
{
    public sealed class DebugViewLogSink : LogSinkBase
    {
        private readonly LogFormatter _formatter;

        public DebugViewLogSink()
            : this(null)
        {
        }

        public DebugViewLogSink(LogFormatter formatter)
        {
            _formatter = formatter;
        }

        public override void Write(LogMessage message)
        {
            if (message == null)
            {
                return;
            }

            Debug.WriteLine(FormatFull(message, _formatter));
        }
    }
}
