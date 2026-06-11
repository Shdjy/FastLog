using System.Diagnostics;
using FastLog.Models;

namespace FastLog.Sinks
{
    public sealed class DebugViewLogSink : LogSinkBase
    {
        public override void Write(LogMessage message)
        {
            if (message == null)
            {
                return;
            }

            Debug.WriteLine(FormatFull(message));
        }
    }
}



