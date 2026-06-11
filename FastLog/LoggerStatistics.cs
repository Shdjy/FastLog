using System.Threading;
using FastLog.Models;

namespace FastLog
{
    internal sealed class LoggerStatistics
    {
        private long _attempted;
        private long _enqueued;
        private long _written;
        private long _dropped;
        private long _sinkErrors;

        public void IncrementAttempted()
        {
            Interlocked.Increment(ref _attempted);
        }

        public void IncrementEnqueued()
        {
            Interlocked.Increment(ref _enqueued);
        }

        public void IncrementWritten()
        {
            Interlocked.Increment(ref _written);
        }

        public void IncrementDropped()
        {
            Interlocked.Increment(ref _dropped);
        }

        public void IncrementSinkErrors()
        {
            Interlocked.Increment(ref _sinkErrors);
        }

        public LoggerStatisticsSnapshot CreateSnapshot(int queueLength, int queueCapacity)
        {
            return new LoggerStatisticsSnapshot(
                Interlocked.Read(ref _attempted),
                Interlocked.Read(ref _enqueued),
                Interlocked.Read(ref _written),
                Interlocked.Read(ref _dropped),
                Interlocked.Read(ref _sinkErrors),
                queueLength,
                queueCapacity);
        }
    }
}
