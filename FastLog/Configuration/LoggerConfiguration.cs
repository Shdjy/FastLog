using System;
using System.Collections.Generic;
using FastLog.Models;
using FastLog.Sinks;

namespace FastLog.Configuration
{
    /// <summary>
    /// Builds a logger instance with runtime options and composable output sinks.
    /// </summary>
    public sealed class LoggerConfiguration
    {
        private readonly List<ILogSink> _sinks;

        public LoggerConfiguration()
        {
            Options = new LoggerOptions();
            _sinks = new List<ILogSink>();
        }

        /// <summary>
        /// Gets the logger runtime options used when creating the logger.
        /// </summary>
        public LoggerOptions Options { get; }

        /// <summary>
        /// Applies custom option changes that are not exposed by a dedicated configuration method.
        /// </summary>
        /// <param name="configureOptions">Action used to modify logger options.</param>
        /// <returns>The current configuration instance.</returns>
        public LoggerConfiguration ConfigureOptions(Action<LoggerOptions> configureOptions)
        {
            if (configureOptions == null)
            {
                throw new ArgumentNullException(nameof(configureOptions));
            }

            configureOptions(Options);
            return this;
        }

        /// <summary>
        /// Sets the project name used by the default log directory resolver.
        /// </summary>
        /// <param name="projectName">Project name.</param>
        /// <returns>The current configuration instance.</returns>
        public LoggerConfiguration ProjectName(string projectName)
        {
            Options.ProjectName = projectName;
            return this;
        }

        /// <summary>
        /// Sets the base path used by the default log directory resolver.
        /// </summary>
        /// <param name="basePath">Base directory path.</param>
        /// <returns>The current configuration instance.</returns>
        public LoggerConfiguration BasePath(string basePath)
        {
            Options.BasePath = basePath;
            return this;
        }

        /// <summary>
        /// Sets the minimum log level that can be written.
        /// </summary>
        /// <param name="level">Minimum log level.</param>
        /// <returns>The current configuration instance.</returns>
        public LoggerConfiguration MinimumLevel(LogLevel level)
        {
            Options.MinimumLevel = level;
            return this;
        }

        /// <summary>
        /// Sets the number of days that old log files are retained.
        /// </summary>
        /// <param name="retentionDays">Retention days. A value less than or equal to zero disables cleanup.</param>
        /// <returns>The current configuration instance.</returns>
        public LoggerConfiguration RetentionDays(int retentionDays)
        {
            Options.RetentionDays = retentionDays;
            return this;
        }

        /// <summary>
        /// Sets the maximum number of messages stored in the asynchronous queue.
        /// </summary>
        /// <param name="queueCapacity">Queue capacity.</param>
        /// <returns>The current configuration instance.</returns>
        public LoggerConfiguration QueueCapacity(int queueCapacity)
        {
            Options.QueueCapacity = queueCapacity;
            return this;
        }

        /// <summary>
        /// Sets the behavior used when the asynchronous queue is full.
        /// </summary>
        /// <param name="queueFullMode">Queue full handling mode.</param>
        /// <returns>The current configuration instance.</returns>
        public LoggerConfiguration QueueFullMode(QueueFullMode queueFullMode)
        {
            Options.QueueFullMode = queueFullMode;
            return this;
        }

        /// <summary>
        /// Sets the number of messages written before a batch flush is requested.
        /// </summary>
        /// <param name="flushBatchSize">Flush batch size.</param>
        /// <returns>The current configuration instance.</returns>
        public LoggerConfiguration FlushBatchSize(int flushBatchSize)
        {
            Options.FlushBatchSize = flushBatchSize;
            return this;
        }

        /// <summary>
        /// Sets the maximum interval between flush operations.
        /// </summary>
        /// <param name="flushIntervalMilliseconds">Flush interval in milliseconds.</param>
        /// <returns>The current configuration instance.</returns>
        public LoggerConfiguration FlushIntervalMilliseconds(int flushIntervalMilliseconds)
        {
            Options.FlushIntervalMilliseconds = flushIntervalMilliseconds;
            return this;
        }

        /// <summary>
        /// Sets the log level that triggers immediate flush.
        /// </summary>
        /// <param name="level">Immediate flush log level.</param>
        /// <returns>The current configuration instance.</returns>
        public LoggerConfiguration ImmediateFlushLevel(LogLevel level)
        {
            Options.ImmediateFlushLevel = level;
            return this;
        }

        /// <summary>
        /// Sets the maximum file size before a file sink rolls to a new file.
        /// </summary>
        /// <param name="maxFileSizeBytes">Maximum file size in bytes. A value less than or equal to zero disables size rolling.</param>
        /// <returns>The current configuration instance.</returns>
        public LoggerConfiguration MaxFileSizeBytes(long maxFileSizeBytes)
        {
            Options.MaxFileSizeBytes = maxFileSizeBytes;
            return this;
        }

        /// <summary>
        /// Sets the directory used by built-in file sinks.
        /// </summary>
        /// <param name="sinkDirectory">Log sink directory.</param>
        /// <returns>The current configuration instance.</returns>
        public LoggerConfiguration SinkDirectory(string sinkDirectory)
        {
            Options.SinkDirectory = sinkDirectory;
            return this;
        }

        /// <summary>
        /// Sets a custom formatter used by built-in sinks.
        /// </summary>
        /// <param name="formatter">Formatter delegate.</param>
        /// <returns>The current configuration instance.</returns>
        public LoggerConfiguration Formatter(Formatting.LogFormatter formatter)
        {
            Options.Formatter = formatter;
            return this;
        }

        /// <summary>
        /// Sets the file name prefix used by file sinks.
        /// </summary>
        /// <param name="fileNamePrefix">File name prefix.</param>
        /// <returns>The current configuration instance.</returns>
        public LoggerConfiguration FileNamePrefix(string fileNamePrefix)
        {
            Options.FileNamePrefix = fileNamePrefix;
            return this;
        }
        /// <summary>
        /// Sets the callback used when a sink write or flush operation fails.
        /// </summary>
        /// <param name="errorHandler">Sink error callback.</param>
        /// <returns>The current configuration instance.</returns>
        public LoggerConfiguration ErrorHandler(Action<Exception> errorHandler)
        {
            Options.ErrorHandler = errorHandler;
            return this;
        }

        /// <summary>
        /// Adds a custom sink to the output pipeline.
        /// </summary>
        /// <param name="sink">Custom sink instance.</param>
        /// <returns>The current configuration instance.</returns>
        public LoggerConfiguration WriteTo(ILogSink sink)
        {
            if (sink == null)
            {
                throw new ArgumentNullException(nameof(sink));
            }

            _sinks.Add(sink);
            return this;
        }

        /// <summary>
        /// Adds a file sink using the configured sink directory and file size settings.
        /// </summary>
        /// <returns>The current configuration instance.</returns>
        public LoggerConfiguration WriteToFile()
        {
            return WriteToFile(Options.ResolveSinkDirectory());
        }

        /// <summary>
        /// Adds a file sink using a specific directory.
        /// </summary>
        /// <param name="directory">Directory where log files are written.</param>
        /// <returns>The current configuration instance.</returns>
        public LoggerConfiguration WriteToFile(string directory)
        {
            return WriteTo(new FileLogSink(directory, Options.MaxFileSizeBytes, Options.Formatter, Options.FileNamePrefix));
        }

        /// <summary>
        /// Adds a console sink.
        /// </summary>
        /// <returns>The current configuration instance.</returns>
        public LoggerConfiguration WriteToConsole()
        {
            return WriteTo(new ConsoleLogSink(Options.Formatter));
        }

        /// <summary>
        /// Adds a DebugView sink.
        /// </summary>
        /// <returns>The current configuration instance.</returns>
        public LoggerConfiguration WriteToDebugView()
        {
            return WriteTo(new DebugViewLogSink(Options.Formatter));
        }

        /// <summary>
        /// Adds a UDP sink.
        /// </summary>
        /// <param name="host">Remote UDP host.</param>
        /// <param name="port">Remote UDP port.</param>
        /// <returns>The current configuration instance.</returns>
        public LoggerConfiguration WriteToUdp(string host, int port)
        {
            Options.UdpHost = host;
            Options.UdpPort = port;
            return WriteTo(new UdpLogSink(host, port, Options.Formatter));
        }

        /// <summary>
        /// Adds a warning-and-above file sink using the configured sink directory and file size settings.
        /// </summary>
        /// <returns>The current configuration instance.</returns>
        public LoggerConfiguration WriteWarningsToFile()
        {
            return WriteWarningsToFile(Options.ResolveSinkDirectory());
        }

        /// <summary>
        /// Adds a warning-and-above file sink using a specific directory.
        /// </summary>
        /// <param name="directory">Directory where warning log files are written.</param>
        /// <returns>The current configuration instance.</returns>
        public LoggerConfiguration WriteWarningsToFile(string directory)
        {
            return WriteTo(new LevelFilterLogSink(new FileLogSink(directory, Options.MaxFileSizeBytes, Options.Formatter, Options.FileNamePrefix), LogLevel.Warn));
        }

        /// <summary>
        /// Creates the configured logger instance.
        /// </summary>
        /// <returns>Configured logger instance.</returns>
        public ILogger CreateLogger()
        {
            ILogSink sink = CreateSink();
            return new Logger(Options, sink);
        }

        private ILogSink CreateSink()
        {
            if (_sinks.Count == 0)
            {
                return Logger.CreateSink(Options);
            }

            if (_sinks.Count == 1)
            {
                return _sinks[0];
            }

            return new CompositeLogSink(_sinks.ToArray());
        }
    }
}

