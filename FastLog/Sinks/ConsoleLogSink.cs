using System;
using FastLog.Formatting;
using FastLog.Models;

namespace FastLog.Sinks
{
    public sealed class ConsoleLogSink : LogSinkBase
    {
        private readonly LogFormatter _formatter;

        public ConsoleLogSink()
            : this(null)
        {
        }

        public ConsoleLogSink(LogFormatter formatter)
        {
            _formatter = formatter;
            WindowsConsoleManager.Open();
        }

        public override void Write(LogMessage message)
        {
            if (message == null)
            {
                return;
            }

            WindowsConsoleManager.WriteLine(FormatShort(message, _formatter), GetColor(message.Level));
        }

        public override void Dispose()
        {
            WindowsConsoleManager.Close();
        }

        private static WindowsConsoleManager.ConsoleTextColor GetColor(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Trace:
                    return WindowsConsoleManager.ConsoleTextColor.DarkGray;
                case LogLevel.Debug:
                    return WindowsConsoleManager.ConsoleTextColor.Gray;
                case LogLevel.Info:
                    return WindowsConsoleManager.ConsoleTextColor.White;
                case LogLevel.Warn:
                    return WindowsConsoleManager.ConsoleTextColor.Yellow;
                case LogLevel.Error:
                    return WindowsConsoleManager.ConsoleTextColor.Red;
                case LogLevel.Fatal:
                    return WindowsConsoleManager.ConsoleTextColor.Magenta;
                default:
                    return WindowsConsoleManager.ConsoleTextColor.White;
            }
        }
    }
}
