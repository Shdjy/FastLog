using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using FastLog.Configuration;
using FastLog.Models;
using FastLog.Sinks;

namespace FastLog
{
    public sealed class Logger : ILogger
    {
        private const int DefaultQueueCapacity = 10000;
        private const int DefaultFlushBatchSize = 128;
        private const int DefaultFlushIntervalMilliseconds = 500;

        private readonly BlockingCollection<LogMessage> _queue;
        private readonly CancellationTokenSource _cancellation;
        private readonly Action<Exception> _errorHandler;
        private readonly Task _worker;
        private readonly object _sinkLock;
        private readonly LoggerOptions _options;
        private readonly LoggerStatistics _statistics;
        private readonly int _queueCapacity;
        private readonly int _flushBatchSize;
        private readonly int _flushIntervalMilliseconds;
        private ILogSink _sink;
        private volatile bool _disposed;

        public Logger(LoggerOptions options)
            : this(options, null)
        {
        }

        public Logger(LoggerOptions options, ILogSink sink)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            MinimumLevel = options.MinimumLevel;
            _options = CloneOptions(options);
            _sink = sink ?? CreateSink(options);
            _errorHandler = options.ErrorHandler;
            _queueCapacity = GetQueueCapacity(options);
            _flushBatchSize = GetFlushBatchSize(options);
            _flushIntervalMilliseconds = GetFlushIntervalMilliseconds(options);
            _queue = new BlockingCollection<LogMessage>(_queueCapacity);
            _cancellation = new CancellationTokenSource();
            _sinkLock = new object();
            _statistics = new LoggerStatistics();

            CleanupOldLogs(options.ResolveSinkDirectory(), options.RetentionDays);
            _worker = Task.Factory.StartNew(
                ProcessQueue,
                _cancellation.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        public LogLevel MinimumLevel { get; set; }

        public void Log(
            LogLevel level,
            string message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if (_disposed || level < MinimumLevel)
            {
                return;
            }

            _statistics.IncrementAttempted();

            LogMessage log = new LogMessage(
                level,
                DateTimeOffset.Now,
                Thread.CurrentThread.ManagedThreadId,
                Path.GetFileName(filePath),
                memberName,
                lineNumber,
                message);

            TryEnqueue(log);
        }

        public void LogFormat(
            LogLevel level,
            string format,
            object[] args,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            Log(level, FormatMessage(format, args), memberName, filePath, lineNumber);
        }
        public void LogException(
            LogLevel level,
            Exception exception,
            string message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if (exception == null)
            {
                Log(level, message, memberName, filePath, lineNumber);
                return;
            }

            string content = string.IsNullOrWhiteSpace(message)
                ? exception.ToString()
                : message + Environment.NewLine + exception;

            Log(level, content, memberName, filePath, lineNumber);
        }

        public void SetLevel(LogLevel level)
        {
            MinimumLevel = level;
            _options.MinimumLevel = level;
        }

        public void SetSink(LogSinkType sinkType)
        {
            LoggerOptions options = CloneOptions(_options);
            options.SinkType = sinkType;
            SetSink(CreateSink(options));
            _options.SinkType = sinkType;
        }

        public void SetSink(ILogSink sink)
        {
            if (sink == null)
            {
                throw new ArgumentNullException(nameof(sink));
            }

            lock (_sinkLock)
            {
                ILogSink oldSink = _sink;
                _sink = sink;
                oldSink.Dispose();
            }
        }

        public LoggerStatisticsSnapshot GetStatistics()
        {
            return _statistics.CreateSnapshot(_queue.Count, _queueCapacity);
        }

        public void Flush()
        {
            lock (_sinkLock)
            {
                while (_queue.TryTake(out LogMessage message))
                {
                    SafeWrite(message);
                }

                SafeFlush();
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _queue.CompleteAdding();

            try
            {
                _worker.Wait();
            }
            finally
            {
                _cancellation.Dispose();
                _queue.Dispose();

                lock (_sinkLock)
                {
                    SafeFlush();
                    _sink.Dispose();
                }
            }
        }

        public static ILogSink CreateSink(LoggerOptions options)
        {
            string directory = options.ResolveSinkDirectory();
            ILogSink warningFileSink = CreateWarningFileSink(options, directory);

            switch (options.SinkType)
            {
                case LogSinkType.File:
                    return new FileLogSink(directory, options.MaxFileSizeBytes, options.Formatter, options.FileNamePrefix);
                case LogSinkType.Console:
                    return new CompositeLogSink(
                        new ConsoleLogSink(options.Formatter),
                        warningFileSink);
                case LogSinkType.DebugView:
                    return new CompositeLogSink(
                        new DebugViewLogSink(options.Formatter),
                        warningFileSink);
                case LogSinkType.Udp:
                    return new CompositeLogSink(
                        warningFileSink,
                        new UdpLogSink(options.UdpHost, options.UdpPort, options.Formatter));
                default:
                    throw new NotSupportedException("Unsupported log sink type: " + options.SinkType);
            }
        }
        private void ProcessQueue()
        {
            int writtenSinceFlush = 0;
            DateTime lastFlushTime = DateTime.UtcNow;

            while (!_queue.IsCompleted)
            {
                if (!_queue.TryTake(out LogMessage message, _flushIntervalMilliseconds))
                {
                    lock (_sinkLock)
                    {
                        FlushIfNeeded(ref writtenSinceFlush, ref lastFlushTime);
                    }

                    continue;
                }

                lock (_sinkLock)
                {
                    WriteMessageAndUpdateFlushState(message, ref writtenSinceFlush, ref lastFlushTime);

                    while (writtenSinceFlush < _flushBatchSize && _queue.TryTake(out LogMessage next))
                    {
                        WriteMessageAndUpdateFlushState(next, ref writtenSinceFlush, ref lastFlushTime);
                    }

                    FlushIfNeeded(ref writtenSinceFlush, ref lastFlushTime);
                }
            }

            lock (_sinkLock)
            {
                while (_queue.TryTake(out LogMessage remaining))
                {
                    SafeWrite(remaining);
                }

                SafeFlush();
            }
        }

        private void WriteMessageAndUpdateFlushState(LogMessage message, ref int writtenSinceFlush, ref DateTime lastFlushTime)
        {
            SafeWrite(message);
            writtenSinceFlush++;

            if (message.Level >= _options.ImmediateFlushLevel)
            {
                SafeFlush();
                writtenSinceFlush = 0;
                lastFlushTime = DateTime.UtcNow;
            }
        }

        private void FlushIfNeeded(ref int writtenSinceFlush, ref DateTime lastFlushTime)
        {
            if (writtenSinceFlush <= 0)
            {
                return;
            }

            if (writtenSinceFlush >= _flushBatchSize || (DateTime.UtcNow - lastFlushTime).TotalMilliseconds >= _flushIntervalMilliseconds)
            {
                SafeFlush();
                writtenSinceFlush = 0;
                lastFlushTime = DateTime.UtcNow;
            }
        }

        private void TryEnqueue(LogMessage log)
        {
            try
            {
                switch (_options.QueueFullMode)
                {
                    case QueueFullMode.Block:
                        _queue.Add(log);
                        _statistics.IncrementEnqueued();
                        break;
                    case QueueFullMode.DropOldest:
                        EnqueueDropOldest(log);
                        break;
                    case QueueFullMode.DropWrite:
                    default:
                        EnqueueDropWrite(log);
                        break;
                }
            }
            catch (InvalidOperationException)
            {
                _statistics.IncrementDropped();
            }
        }

        private void EnqueueDropWrite(LogMessage log)
        {
            if (_queue.TryAdd(log))
            {
                _statistics.IncrementEnqueued();
                return;
            }

            _statistics.IncrementDropped();
        }

        private void EnqueueDropOldest(LogMessage log)
        {
            if (_queue.TryAdd(log))
            {
                _statistics.IncrementEnqueued();
                return;
            }

            if (_queue.TryTake(out LogMessage _))
            {
                _statistics.IncrementDropped();
            }

            if (_queue.TryAdd(log))
            {
                _statistics.IncrementEnqueued();
                return;
            }

            _statistics.IncrementDropped();
        }

        private void SafeWrite(LogMessage message)
        {
            try
            {
                _sink.Write(message);
                _statistics.IncrementWritten();
            }
            catch (Exception ex)
            {
                _statistics.IncrementSinkErrors();
                HandleSinkException(ex);
            }
        }

        private void SafeFlush()
        {
            try
            {
                _sink.Flush();
            }
            catch (Exception ex)
            {
                _statistics.IncrementSinkErrors();
                HandleSinkException(ex);
            }
        }

        private void HandleSinkException(Exception exception)
        {
            if (_errorHandler == null)
            {
                return;
            }

            try
            {
                _errorHandler(exception);
            }
            catch
            {
            }
        }

        private static string FormatMessage(string format, object[] args)
        {
            if (format == null)
            {
                return string.Empty;
            }

            if (args == null || args.Length == 0)
            {
                return format;
            }

            try
            {
                return string.Format(CultureInfo.CurrentCulture, format, args);
            }
            catch (FormatException)
            {
                return format + " " + string.Join(" ", args);
            }
        }

        private static int GetQueueCapacity(LoggerOptions options)
        {
            if (options.QueueCapacity > 0)
            {
                return options.QueueCapacity;
            }

            return DefaultQueueCapacity;
        }

        private static int GetFlushBatchSize(LoggerOptions options)
        {
            if (options.FlushBatchSize > 0)
            {
                return options.FlushBatchSize;
            }

            return DefaultFlushBatchSize;
        }

        private static int GetFlushIntervalMilliseconds(LoggerOptions options)
        {
            if (options.FlushIntervalMilliseconds > 0)
            {
                return options.FlushIntervalMilliseconds;
            }

            return DefaultFlushIntervalMilliseconds;
        }

        private static ILogSink CreateWarningFileSink(LoggerOptions options, string directory)
        {
            return new LevelFilterLogSink(new FileLogSink(directory, options.MaxFileSizeBytes, options.Formatter, options.FileNamePrefix), LogLevel.Warn);
        }

        private static LoggerOptions CloneOptions(LoggerOptions options)
        {
            return new LoggerOptions
            {
                ProjectName = options.ProjectName,
                BasePath = options.BasePath,
                MinimumLevel = options.MinimumLevel,
                SinkType = options.SinkType,
                RetentionDays = options.RetentionDays,
                QueueCapacity = options.QueueCapacity,
                QueueFullMode = options.QueueFullMode,
                FlushBatchSize = options.FlushBatchSize,
                FlushIntervalMilliseconds = options.FlushIntervalMilliseconds,
                ImmediateFlushLevel = options.ImmediateFlushLevel,
                MaxFileSizeBytes = options.MaxFileSizeBytes,
                ErrorHandler = options.ErrorHandler,
                SinkDirectory = options.SinkDirectory,
                UdpHost = options.UdpHost,
                UdpPort = options.UdpPort
            };
        }

        private static void CleanupOldLogs(string directory, int retentionDays)
        {
            if (retentionDays <= 0 || string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            {
                return;
            }

            DateTime threshold = DateTime.Now.AddDays(-retentionDays);

            foreach (string file in Directory.GetFiles(directory, "*.log", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    if (File.GetLastWriteTime(file) < threshold)
                    {
                        File.Delete(file);
                    }
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
        }
    }
}





