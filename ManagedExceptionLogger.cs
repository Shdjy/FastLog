using System;
using System.Threading.Tasks;
using FastLog.Models;

namespace FastLog
{
    public sealed class ManagedExceptionLogger : IDisposable
    {
        private readonly ILogger _logger;
        private bool _disposed;

        public ManagedExceptionLogger(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Install()
        {
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
            TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception exception = e.ExceptionObject as Exception;

            if (exception != null)
            {
                _logger.LogException(LogLevel.Fatal, exception, "Unhandled application exception.", string.Empty, string.Empty, 0);
            }
            else
            {
                _logger.Log(LogLevel.Fatal, "Unhandled application exception: " + e.ExceptionObject, string.Empty, string.Empty, 0);
            }

            _logger.Flush();
        }

        private void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            _logger.LogException(LogLevel.Error, e.Exception, "Unobserved task exception.", string.Empty, string.Empty, 0);
            _logger.Flush();
        }
    }
}



