using System;
using FastLog.Models;

namespace FastLog.Configuration
{
    /// <summary>
    /// Configures the logger runtime, output target and file retention behavior.
    /// </summary>
    public sealed class LoggerOptions
    {
        public LoggerOptions()
        {
            ProjectName = "FastLog";
            BasePath = AppDomain.CurrentDomain.BaseDirectory;
            MinimumLevel = LogLevel.Info;
            SinkType = LogSinkType.File;
            RetentionDays = 30;
            QueueCapacity = 10000;
            QueueFullMode = QueueFullMode.DropWrite;
            FlushBatchSize = 128;
            FlushIntervalMilliseconds = 500;
            ImmediateFlushLevel = LogLevel.Error;
            MaxFileSizeBytes = 0;
            UdpHost = "127.0.0.1";
            UdpPort = 0;
        }

        public string ProjectName { get; set; }

        public string BasePath { get; set; }

        public LogLevel MinimumLevel { get; set; }

        public LogSinkType SinkType { get; set; }

        public int RetentionDays { get; set; }

        public int QueueCapacity { get; set; }

        public QueueFullMode QueueFullMode { get; set; }

        public int FlushBatchSize { get; set; }

        public int FlushIntervalMilliseconds { get; set; }

        public LogLevel ImmediateFlushLevel { get; set; }

        public long MaxFileSizeBytes { get; set; }

        public Action<Exception> ErrorHandler { get; set; }

        public string SinkDirectory { get; set; }

        public string UdpHost { get; set; }

        public int UdpPort { get; set; }

        public string ResolveSinkDirectory()
        {
            if (!string.IsNullOrWhiteSpace(SinkDirectory))
            {
                return SinkDirectory;
            }

            return System.IO.Path.Combine(BasePath ?? AppDomain.CurrentDomain.BaseDirectory, ProjectName ?? "FastLog");
        }
    }
}



