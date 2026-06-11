using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FastLog;
using FastLog.Configuration;
using FastLog.Models;

namespace FastLog.Benchmark
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;

            int exitCode = 0;

            try
            {
                BenchmarkOptions options = args != null && args.Length > 0
                    ? BenchmarkOptions.Parse(args)
                    : BenchmarkOptions.ReadFromConsole();

                RunBenchmark(options);
            }
            catch (Exception ex)
            {
                exitCode = 2;
                Console.WriteLine("压测执行失败：");
                Console.WriteLine(ex);
            }
            finally
            {
                Console.WriteLine();
                Console.WriteLine("按任意键退出...");
                Console.ReadKey(true);
            }

            return exitCode;
        }

        private static void RunBenchmark(BenchmarkOptions options)
        {
            PrintOptions(options);
            Directory.CreateDirectory(options.LogPath);

            int gen0Before = GC.CollectionCount(0);
            int gen1Before = GC.CollectionCount(1);
            int gen2Before = GC.CollectionCount(2);
            long memoryBefore = GC.GetTotalMemory(true);

            Log.Initialize(new LoggerOptions
            {
                ProjectName = "FastLogBenchmark",
                BasePath = options.LogPath,
                MinimumLevel = LogLevel.Info,
                SinkType = options.SinkType,
                QueueCapacity = options.QueueCapacity,
                QueueFullMode = options.QueueFullMode,
                FlushBatchSize = options.FlushBatchSize,
                FlushIntervalMilliseconds = options.FlushIntervalMilliseconds,
                ImmediateFlushLevel = LogLevel.Fatal,
                MaxFileSizeBytes = options.MaxFileSizeBytes,
                UdpHost = options.UdpHost,
                UdpPort = options.UdpPort
            });

            Stopwatch enqueueWatch = Stopwatch.StartNew();

            Parallel.For(
                0,
                options.ThreadCount,
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = options.ThreadCount
                },
                threadIndex =>
                {
                    for (int i = 0; i < options.LogsPerThread; i++)
                    {
                        Log.Info("性能测试日志 thread=" + threadIndex.ToString(CultureInfo.InvariantCulture) + " index=" + i.ToString(CultureInfo.InvariantCulture));
                    }
                });

            enqueueWatch.Stop();

            Stopwatch drainWatch = Stopwatch.StartNew();
            Log.Flush();
            drainWatch.Stop();

            LoggerStatisticsSnapshot finalStatistics = Log.GetStatistics();

            Stopwatch shutdownWatch = Stopwatch.StartNew();
            Log.Shutdown();
            shutdownWatch.Stop();

            long memoryAfter = GC.GetTotalMemory(true);
            int gen0After = GC.CollectionCount(0);
            int gen1After = GC.CollectionCount(1);
            int gen2After = GC.CollectionCount(2);

            PrintResult(
                options,
                finalStatistics,
                enqueueWatch.ElapsedMilliseconds,
                drainWatch.ElapsedMilliseconds,
                shutdownWatch.ElapsedMilliseconds,
                gen0After - gen0Before,
                gen1After - gen1Before,
                gen2After - gen2Before,
                memoryAfter - memoryBefore);
        }

        private static void PrintOptions(BenchmarkOptions options)
        {
            Console.WriteLine("FastLog 日志库性能压测");
            Console.WriteLine("----------------------------------------");
            Console.WriteLine("生产线程数：" + options.ThreadCount);
            Console.WriteLine("每线程日志数：" + options.LogsPerThread);
            Console.WriteLine("总日志数：" + options.TotalLogs);
            Console.WriteLine("输出类型：" + options.SinkType);
            Console.WriteLine("队列容量：" + options.QueueCapacity);
            Console.WriteLine("队列满策略：" + options.QueueFullMode);
            Console.WriteLine("批量 Flush 条数：" + options.FlushBatchSize);
            Console.WriteLine("Flush 间隔毫秒：" + options.FlushIntervalMilliseconds);
            Console.WriteLine("文件滚动大小：" + options.MaxFileSizeBytes + " 字节");
            Console.WriteLine("日志目录：" + options.LogPath);
            Console.WriteLine("----------------------------------------");
            Console.WriteLine("开始压测...");
            Console.WriteLine();
        }

        private static void PrintResult(
            BenchmarkOptions options,
            LoggerStatisticsSnapshot statistics,
            long enqueueElapsedMs,
            long drainElapsedMs,
            long shutdownElapsedMs,
            int gen0Count,
            int gen1Count,
            int gen2Count,
            long memoryDeltaBytes)
        {
            double enqueueLogsPerSecond = options.TotalLogs * 1000.0 / Math.Max(1, enqueueElapsedMs);
            double totalLogsPerSecond = options.TotalLogs * 1000.0 / Math.Max(1, enqueueElapsedMs + drainElapsedMs + shutdownElapsedMs);

            Console.WriteLine("压测结果");
            Console.WriteLine("----------------------------------------");
            Console.WriteLine("生产者写入耗时：" + enqueueElapsedMs + " ms");
            Console.WriteLine("队列排空耗时：" + drainElapsedMs + " ms");
            Console.WriteLine("关闭释放耗时：" + shutdownElapsedMs + " ms");
            Console.WriteLine("入队吞吐量：" + enqueueLogsPerSecond.ToString("F2", CultureInfo.InvariantCulture) + " 条/秒");
            Console.WriteLine("端到端吞吐量：" + totalLogsPerSecond.ToString("F2", CultureInfo.InvariantCulture) + " 条/秒");
            Console.WriteLine("尝试写入数量：" + statistics.Attempted);
            Console.WriteLine("成功入队数量：" + statistics.Enqueued);
            Console.WriteLine("实际写入数量：" + statistics.Written);
            Console.WriteLine("丢弃日志数量：" + statistics.Dropped);
            Console.WriteLine("Sink异常数量：" + statistics.SinkErrors);
            Console.WriteLine("Flush后队列剩余：" + statistics.QueueLength);
            Console.WriteLine("GC Gen0 次数：" + gen0Count);
            Console.WriteLine("GC Gen1 次数：" + gen1Count);
            Console.WriteLine("GC Gen2 次数：" + gen2Count);
            Console.WriteLine("内存变化：" + memoryDeltaBytes + " 字节");
            Console.WriteLine("----------------------------------------");
        }
    }

    internal sealed class BenchmarkOptions
    {
        public BenchmarkOptions()
        {
            ThreadCount = Environment.ProcessorCount;
            LogsPerThread = 100000;
            SinkType = LogSinkType.File;
            QueueCapacity = 100000;
            QueueFullMode = QueueFullMode.Block;
            FlushBatchSize = 512;
            FlushIntervalMilliseconds = 1000;
            MaxFileSizeBytes = 0;
            UdpHost = "127.0.0.1";
            UdpPort = 9000;
            LogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BenchmarkLogs");
        }

        public int ThreadCount { get; private set; }

        public int LogsPerThread { get; private set; }

        public int TotalLogs
        {
            get
            {
                return ThreadCount * LogsPerThread;
            }
        }

        public LogSinkType SinkType { get; private set; }

        public int QueueCapacity { get; private set; }

        public QueueFullMode QueueFullMode { get; private set; }

        public int FlushBatchSize { get; private set; }

        public int FlushIntervalMilliseconds { get; private set; }

        public long MaxFileSizeBytes { get; private set; }

        public string UdpHost { get; private set; }

        public int UdpPort { get; private set; }

        public string LogPath { get; private set; }

        public static BenchmarkOptions ReadFromConsole()
        {
            BenchmarkOptions options = new BenchmarkOptions();

            Console.WriteLine("FastLog 日志库性能压测参数设置");
            Console.WriteLine("直接回车使用默认值。");
            Console.WriteLine();

            options.ThreadCount = ReadInt("生产线程数", options.ThreadCount);
            options.LogsPerThread = ReadInt("每线程日志数", options.LogsPerThread);
            options.SinkType = ReadEnum("输出类型 File/Console/DebugView/Udp", options.SinkType);
            options.QueueCapacity = ReadInt("队列容量", options.QueueCapacity);
            options.QueueFullMode = ReadEnum("队列满策略 DropWrite/Block/DropOldest", options.QueueFullMode);
            options.FlushBatchSize = ReadInt("批量 Flush 条数", options.FlushBatchSize);
            options.FlushIntervalMilliseconds = ReadInt("Flush 间隔毫秒", options.FlushIntervalMilliseconds);
            options.MaxFileSizeBytes = ReadLong("文件滚动大小字节，0表示关闭", options.MaxFileSizeBytes);
            options.LogPath = ReadString("日志目录", options.LogPath);

            if (options.SinkType == LogSinkType.Udp)
            {
                options.UdpHost = ReadString("UDP地址", options.UdpHost);
                options.UdpPort = ReadInt("UDP端口", options.UdpPort);
            }

            Console.WriteLine();
            return options;
        }

        public static BenchmarkOptions Parse(string[] args)
        {
            BenchmarkOptions options = new BenchmarkOptions();

            foreach (string arg in args ?? Array.Empty<string>())
            {
                string[] parts = arg.Split(new[] { '=' }, 2);

                if (parts.Length != 2)
                {
                    continue;
                }

                string name = parts[0].TrimStart('-', '/');
                string value = parts[1];

                if (string.Equals(name, "threads", StringComparison.OrdinalIgnoreCase))
                {
                    options.ThreadCount = ParseInt(value, options.ThreadCount);
                }
                else if (string.Equals(name, "logs", StringComparison.OrdinalIgnoreCase))
                {
                    options.LogsPerThread = ParseInt(value, options.LogsPerThread);
                }
                else if (string.Equals(name, "sink", StringComparison.OrdinalIgnoreCase))
                {
                    options.SinkType = ParseEnum(value, options.SinkType);
                }
                else if (string.Equals(name, "queue", StringComparison.OrdinalIgnoreCase))
                {
                    options.QueueCapacity = ParseInt(value, options.QueueCapacity);
                }
                else if (string.Equals(name, "queueMode", StringComparison.OrdinalIgnoreCase))
                {
                    options.QueueFullMode = ParseEnum(value, options.QueueFullMode);
                }
                else if (string.Equals(name, "flushBatch", StringComparison.OrdinalIgnoreCase))
                {
                    options.FlushBatchSize = ParseInt(value, options.FlushBatchSize);
                }
                else if (string.Equals(name, "flushMs", StringComparison.OrdinalIgnoreCase))
                {
                    options.FlushIntervalMilliseconds = ParseInt(value, options.FlushIntervalMilliseconds);
                }
                else if (string.Equals(name, "maxFileBytes", StringComparison.OrdinalIgnoreCase))
                {
                    options.MaxFileSizeBytes = ParseLong(value, options.MaxFileSizeBytes);
                }
                else if (string.Equals(name, "udpHost", StringComparison.OrdinalIgnoreCase))
                {
                    options.UdpHost = value;
                }
                else if (string.Equals(name, "udpPort", StringComparison.OrdinalIgnoreCase))
                {
                    options.UdpPort = ParseInt(value, options.UdpPort);
                }
                else if (string.Equals(name, "path", StringComparison.OrdinalIgnoreCase))
                {
                    options.LogPath = value;
                }
            }

            return options;
        }

        private static int ReadInt(string name, int defaultValue)
        {
            string value = ReadString(name, defaultValue.ToString(CultureInfo.InvariantCulture));
            return ParseInt(value, defaultValue);
        }

        private static long ReadLong(string name, long defaultValue)
        {
            string value = ReadString(name, defaultValue.ToString(CultureInfo.InvariantCulture));
            return ParseLong(value, defaultValue);
        }

        private static string ReadString(string name, string defaultValue)
        {
            Console.Write(name + " [" + defaultValue + "]：");
            string value = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(value))
            {
                return defaultValue;
            }

            return value.Trim();
        }

        private static T ReadEnum<T>(string name, T defaultValue)
            where T : struct
        {
            string value = ReadString(name, defaultValue.ToString());
            return ParseEnum(value, defaultValue);
        }

        private static int ParseInt(string value, int fallback)
        {
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result)
                ? result
                : fallback;
        }

        private static long ParseLong(string value, long fallback)
        {
            return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long result)
                ? result
                : fallback;
        }

        private static T ParseEnum<T>(string value, T fallback)
            where T : struct
        {
            return Enum.TryParse(value, true, out T result)
                ? result
                : fallback;
        }
    }
}
