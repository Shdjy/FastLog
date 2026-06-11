using System;
using System.Collections.Generic;
using FastLog.Models;

namespace FastLog.Sinks
{
    public sealed class CompositeLogSink : LogSinkBase
    {
        private readonly IReadOnlyList<ILogSink> _sinks;

        public CompositeLogSink(params ILogSink[] sinks)
        {
            _sinks = sinks ?? Array.Empty<ILogSink>();
        }

        public override void Write(LogMessage message)
        {
            foreach (ILogSink sink in _sinks)
            {
                sink.Write(message);
            }
        }

        public override void Flush()
        {
            foreach (ILogSink sink in _sinks)
            {
                sink.Flush();
            }
        }

        public override void Dispose()
        {
            foreach (ILogSink sink in _sinks)
            {
                sink.Dispose();
            }
        }
    }
}



