using System;
using System.Runtime.InteropServices;

namespace FastLog.Sinks
{
    internal static class WindowsConsoleManager
    {
        private const int AttachParentProcess = -1;
        private const int GenericWrite = 0x40000000;
        private const int FileShareWrite = 0x00000002;
        private const int Existing = 3;
        private const int FileAttributeNormal = 0x00000080;
        private const uint ScClose = 0xF060;
        private const uint MfByCommand = 0x00000000;
        private const uint MfGrayed = 0x00000001;

        private static readonly object SyncRoot = new object();
        private static readonly IntPtr InvalidHandleValue = new IntPtr(-1);
        private static readonly ConsoleCtrlDelegate CtrlHandler = HandleConsoleControl;
        private static bool _initialized;
        private static bool _createdByFastLog;
        private static int _referenceCount;
        private static IntPtr _outputHandle;

        public static void Open()
        {
            lock (SyncRoot)
            {
                _referenceCount++;

                if (_initialized)
                {
                    return;
                }

                if (IsWindows())
                {
                    EnsureWindowsConsole();
                    _outputHandle = OpenConsoleOutput();
                    DisableCloseButton();
                    SetConsoleCtrlHandler(CtrlHandler, true);
                }

                _initialized = true;
            }
        }

        public static void Close()
        {
            lock (SyncRoot)
            {
                if (_referenceCount > 0)
                {
                    _referenceCount--;
                }

                if (_referenceCount > 0 || !_initialized)
                {
                    return;
                }

                if (IsWindows())
                {
                    SetConsoleCtrlHandler(CtrlHandler, false);
                    CloseConsoleOutput();

                    if (_createdByFastLog)
                    {
                        FreeConsole();
                    }
                }

                _createdByFastLog = false;
                _initialized = false;
            }
        }

        public static void WriteLine(string text, ConsoleTextColor color)
        {
            if (!IsWindows())
            {
                Console.WriteLine(text);
                return;
            }

            if (!_initialized)
            {
                Open();
            }

            lock (SyncRoot)
            {
                if (!IsValidHandle(_outputHandle))
                {
                    _outputHandle = OpenConsoleOutput();
                }

                if (!IsValidHandle(_outputHandle))
                {
                    return;
                }

                ushort oldAttributes = 0;

                if (GetConsoleScreenBufferInfo(_outputHandle, out ConsoleScreenBufferInfo info))
                {
                    oldAttributes = info.Attributes;
                }

                SetConsoleTextAttribute(_outputHandle, ToWin32Color(color));

                string content = (text ?? string.Empty) + Environment.NewLine;
                WriteConsole(_outputHandle, content, content.Length, out int _, IntPtr.Zero);

                if (oldAttributes != 0)
                {
                    SetConsoleTextAttribute(_outputHandle, oldAttributes);
                }
            }
        }

        private static void EnsureWindowsConsole()
        {
            if (GetConsoleWindow() != IntPtr.Zero)
            {
                return;
            }

            if (!AttachConsole(AttachParentProcess))
            {
                _createdByFastLog = AllocConsole();
            }
        }

        private static IntPtr OpenConsoleOutput()
        {
            if (GetConsoleWindow() == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }

            return CreateFile(
                "CONOUT$",
                GenericWrite,
                FileShareWrite,
                IntPtr.Zero,
                Existing,
                FileAttributeNormal,
                IntPtr.Zero);
        }

        private static void CloseConsoleOutput()
        {
            if (!IsValidHandle(_outputHandle))
            {
                _outputHandle = IntPtr.Zero;
                return;
            }

            CloseHandle(_outputHandle);
            _outputHandle = IntPtr.Zero;
        }

        private static bool IsValidHandle(IntPtr handle)
        {
            return handle != IntPtr.Zero && handle != InvalidHandleValue;
        }

        private static void DisableCloseButton()
        {
            IntPtr consoleWindow = GetConsoleWindow();

            if (consoleWindow == IntPtr.Zero)
            {
                return;
            }

            IntPtr systemMenu = GetSystemMenu(consoleWindow, false);

            if (systemMenu == IntPtr.Zero)
            {
                return;
            }

            EnableMenuItem(systemMenu, ScClose, MfByCommand | MfGrayed);
        }

        private static ushort ToWin32Color(ConsoleTextColor color)
        {
            switch (color)
            {
                case ConsoleTextColor.DarkGray:
                    return 0x0008;
                case ConsoleTextColor.Gray:
                    return 0x0007;
                case ConsoleTextColor.White:
                    return 0x000F;
                case ConsoleTextColor.Yellow:
                    return 0x000E;
                case ConsoleTextColor.Red:
                    return 0x000C;
                case ConsoleTextColor.Magenta:
                    return 0x000D;
                default:
                    return 0x0007;
            }
        }

        private static bool HandleConsoleControl(int controlType)
        {
            return true;
        }

        private static bool IsWindows()
        {
            return Environment.OSVersion.Platform == PlatformID.Win32NT;
        }

        private delegate bool ConsoleCtrlDelegate(int controlType);

        internal enum ConsoleTextColor
        {
            DarkGray,
            Gray,
            White,
            Yellow,
            Red,
            Magenta
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Coord
        {
            public short X;
            public short Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SmallRect
        {
            public short Left;
            public short Top;
            public short Right;
            public short Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ConsoleScreenBufferInfo
        {
            public Coord Size;
            public Coord CursorPosition;
            public ushort Attributes;
            public SmallRect Window;
            public Coord MaximumWindowSize;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate handlerRoutine, bool add);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleTextAttribute(IntPtr consoleOutput, ushort attributes);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetConsoleScreenBufferInfo(IntPtr consoleOutput, out ConsoleScreenBufferInfo info);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool WriteConsole(IntPtr consoleOutput, string buffer, int length, out int written, IntPtr reserved);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateFile(
            string fileName,
            int desiredAccess,
            int shareMode,
            IntPtr securityAttributes,
            int creationDisposition,
            int flagsAndAttributes,
            IntPtr templateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr handle);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetSystemMenu(IntPtr windowHandle, bool revert);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint EnableMenuItem(IntPtr menuHandle, uint menuItemId, uint enable);
    }
}
