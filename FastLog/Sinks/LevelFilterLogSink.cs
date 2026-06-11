using System;
using FastLog.Models;

namespace FastLog.Sinks
{
    public sealed class LevelFilterLogSink : LogSinkBase
    {
        private readonly ILogSink _innerSink;
        private readonly LogLevel _minimumLevel;

        public LevelFilterLogSink(ILogSink innerSink, LogLevel minimumLevel)
        {
            _innerSink = innerSink ?? throw new ArgumentNullException(nameof(innerSink));
            _minimumLevel = minimumLevel;
        }

        public override void Write(LogMessage message)
        {
            if (message == null || message.Level < _minimumLevel)
            {
                return;
            }

            _innerSink.Write(message);
        }

        public override void Flush()
        {
            _innerSink.Flush();
        }

        public override void Dispose()
        {
            _innerSink.Dispose();
        }
    }
}
