using System;
using System.Globalization;
using System.IO;
using System.Text;
using FastLog.Models;

namespace FastLog.Sinks
{
    public sealed class FileLogSink : LogSinkBase
    {
        private readonly object _syncRoot;
        private readonly string _directory;
        private readonly long _maxFileSizeBytes;
        private StreamWriter _writer;
        private string _currentDate;
        private int _currentIndex;
        private string _currentPath;

        public FileLogSink(string directory)
            : this(directory, 0)
        {
        }

        public FileLogSink(string directory, long maxFileSizeBytes)
        {
            _syncRoot = new object();
            _directory = directory ?? throw new ArgumentNullException(nameof(directory));
            _maxFileSizeBytes = maxFileSizeBytes;
        }

        public override void Write(LogMessage message)
        {
            if (message == null)
            {
                return;
            }

            lock (_syncRoot)
            {
                OpenLogFileIfNeeded(message.Timestamp);
                _writer.WriteLine(FormatFull(message));
            }
        }

        public override void Flush()
        {
            lock (_syncRoot)
            {
                if (_writer != null)
                {
                    _writer.Flush();
                }
            }
        }

        public override void Dispose()
        {
            lock (_syncRoot)
            {
                if (_writer != null)
                {
                    _writer.Flush();
                    _writer.Dispose();
                    _writer = null;
                }
            }
        }

        private void OpenLogFileIfNeeded(DateTimeOffset timestamp)
        {
            string date = timestamp.LocalDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            if (_writer != null && string.Equals(_currentDate, date, StringComparison.Ordinal) && !ShouldRollBySize())
            {
                return;
            }

            if (!string.Equals(_currentDate, date, StringComparison.Ordinal))
            {
                _currentDate = date;
                _currentIndex = 0;
            }
            else if (_writer != null && ShouldRollBySize())
            {
                _currentIndex++;
            }

            CloseCurrentWriter();
            Directory.CreateDirectory(_directory);

            _currentPath = BuildLogFilePath(_currentDate, _currentIndex);

            while (_maxFileSizeBytes > 0 && File.Exists(_currentPath) && new FileInfo(_currentPath).Length >= _maxFileSizeBytes)
            {
                _currentIndex++;
                _currentPath = BuildLogFilePath(_currentDate, _currentIndex);
            }

            FileStream stream = new FileStream(_currentPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            _writer = new StreamWriter(stream, Encoding.UTF8);
        }

        private bool ShouldRollBySize()
        {
            if (_maxFileSizeBytes <= 0 || string.IsNullOrWhiteSpace(_currentPath) || !File.Exists(_currentPath))
            {
                return false;
            }

            return new FileInfo(_currentPath).Length >= _maxFileSizeBytes;
        }

        private string BuildLogFilePath(string date, int index)
        {
            if (index <= 0)
            {
                return Path.Combine(_directory, date + ".log");
            }

            return Path.Combine(_directory, date + "_" + index.ToString(CultureInfo.InvariantCulture) + ".log");
        }

        private void CloseCurrentWriter()
        {
            if (_writer == null)
            {
                return;
            }

            _writer.Flush();
            _writer.Dispose();
            _writer = null;
        }
    }
}
