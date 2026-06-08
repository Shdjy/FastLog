namespace FastLog.Models
{
    /// <summary>
    /// Represents logger runtime counters captured at one moment.
    /// </summary>
    public sealed class LoggerStatisticsSnapshot
    {
        public LoggerStatisticsSnapshot(
            long attempted,
            long enqueued,
            long written,
            long dropped,
            long sinkErrors,
            int queueLength,
            int queueCapacity)
        {
            Attempted = attempted;
            Enqueued = enqueued;
            Written = written;
            Dropped = dropped;
            SinkErrors = sinkErrors;
            QueueLength = queueLength;
            QueueCapacity = queueCapacity;
        }

        public long Attempted { get; }

        public long Enqueued { get; }

        public long Written { get; }

        public long Dropped { get; }

        public long SinkErrors { get; }

        public int QueueLength { get; }

        public int QueueCapacity { get; }
    }
}
