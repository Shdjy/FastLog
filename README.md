# FastLog 使用说明

FastLog 是一个 C# 异步日志库，参考 `CppLog/Clog` 的设计思路实现。

核心能力：

- 异步队列写日志，降低业务线程阻塞
- 支持文件、控制台、DebugView、UDP 输出
- 支持日志等级过滤
- 自动记录时间、等级、文件名、方法名、行号、线程 ID
- 支持队列满策略
- 支持批量 Flush 和定时 Flush
- 支持文件按日期生成和按大小滚动
- 支持运行时统计
- 控制台、DebugView、UDP 这类临时输出会把 `Warn`、`Error`、`Fatal` 额外写入文件，避免关键错误丢失
- 控制台程序使用控制台输出时复用当前控制台，不禁用控制台窗口关闭按钮

FastLog 主要有四种使用方式：

- 静态 `Log` 类
- 通过 `LoggerConfiguration` 组合多个 Sink
- 创建 `Logger` 对象
- 通过 `ILogger` 依赖注入

## 1. 静态 Log 类

这是最简单的使用方式，适合一个程序只有一个全局日志实例的场景。

优点：

- 使用简单
- 自动记录调用方法、文件名、行号
- 适合 WinForms、控制台、服务程序快速接入

示例：

```csharp
using FastLog;
using FastLog.Configuration;
using FastLog.Models;

Log.Initialize(new LoggerOptions
{
    ProjectName = "贴标机",
    BasePath = @"D:\Logs",
    MinimumLevel = LogLevel.Info,
    SinkType = LogSinkType.File,
    RetentionDays = 30,
    QueueCapacity = 100000,
    QueueFullMode = QueueFullMode.Block,
    FlushBatchSize = 512,
    FlushIntervalMilliseconds = 1000,
    ImmediateFlushLevel = LogLevel.Error,
    MaxFileSizeBytes = 100 * 1024 * 1024
});

Log.Info("程序启动");
Log.Warn("气压偏低");
Log.Error("PLC连接失败");

Log.Shutdown();
```

日志文件目录：

```text
D:\Logs\贴标机\yyyy-MM-dd.log
```

异常日志：

```csharp
try
{
    throw new InvalidOperationException("测试异常");
}
catch (Exception ex)
{
    Log.Error(ex, "业务处理异常");
}
```

## 2. 使用 LoggerConfiguration 组合多个 Sink

`LoggerConfiguration` 用于把日志运行参数和多个输出目标组合在一起。它解决的是 `LoggerOptions.SinkType` 只能选择一种内置输出方式的问题。

适合场景：

- 同时写入文件、控制台、DebugView、UDP
- 一个日志实例需要多个输出目标
- 需要接入自定义 `ILogSink`
- 希望新增 Sink 时不修改 `Logger` 内部代码

示例：

```csharp
using FastLog;
using FastLog.Configuration;
using FastLog.Models;

Log.Initialize(config =>
{
    config
        .ProjectName("贴标机")
        .BasePath(@"D:\Logs")
        .MinimumLevel(LogLevel.Info)
        .RetentionDays(30)
        .QueueCapacity(100000)
        .QueueFullMode(QueueFullMode.Block)
        .FlushBatchSize(512)
        .FlushIntervalMilliseconds(1000)
        .ImmediateFlushLevel(LogLevel.Error)
        .MaxFileSizeBytes(100 * 1024 * 1024)
        .WriteToFile()
        .WriteToConsole();
});

Log.Info("程序启动");
Log.Warn("气压偏低");
Log.Error("PLC连接失败");

Log.Shutdown();
```

常用输出组合：

```csharp
Log.Initialize(config =>
{
    config
        .MinimumLevel(LogLevel.Debug)
        .WriteToFile(@"D:\Logs\Main")
        .WriteToConsole()
        .WriteToDebugView();
});
```

UDP 输出组合：

```csharp
Log.Initialize(config =>
{
    config
        .ProjectName("贴标机")
        .BasePath(@"D:\Logs")
        .MinimumLevel(LogLevel.Info)
        .WriteToFile()
        .WriteToUdp("127.0.0.1", 9000);
});
```

警告级别单独写入文件：

```csharp
Log.Initialize(config =>
{
    config
        .WriteToFile(@"D:\Logs\All")
        .WriteWarningsToFile(@"D:\Logs\Warnings");
});
```

接入自定义 Sink：

```csharp
Log.Initialize(config =>
{
    config
        .MinimumLevel(LogLevel.Info)
        .WriteToFile()
        .WriteTo(new MesLogSink());
});
```

如果没有调用任何 `WriteTo...` 方法，`LoggerConfiguration` 会继续使用原来的默认输出逻辑，也就是根据 `LoggerOptions.SinkType` 创建内置 Sink。因此旧代码和默认行为不会受到影响。
## 3. 创建 Logger 对象

这种方式适合一个程序里需要多个独立日志实例的场景。

适合场景：

- 每个模块单独一个日志目录
- Camera、PLC、MES 等模块分别记录日志
- 不想使用全局静态日志
- 需要手动管理日志生命周期

示例：

```csharp
using FastLog;
using FastLog.Configuration;
using FastLog.Models;

using (Logger cameraLogger = new Logger(new LoggerOptions
{
    ProjectName = "CameraModule",
    BasePath = @"D:\Logs",
    MinimumLevel = LogLevel.Debug,
    SinkType = LogSinkType.File,
    QueueFullMode = QueueFullMode.Block
}))
{
    cameraLogger.Log(LogLevel.Info, "相机模块启动", nameof(Main), "Program.cs", 20);
    cameraLogger.Log(LogLevel.Warn, "相机响应变慢", nameof(Main), "Program.cs", 21);
    cameraLogger.Flush();
}
```

注意：直接调用 `Logger.Log(...)` 时，需要显式传入方法名、文件名和行号：

```csharp
logger.Log(LogLevel.Info, "日志内容", "方法名", "文件名.cs", 100);
```

## 4. Logger 再封装一层并保留文件名行号

如果你觉得每次手动传方法名、文件名、行号太麻烦，可以给 `Logger` 再封装一层。

关键点是：封装方法上继续使用 C# 的调用方信息特性：

- `[CallerMemberName]`
- `[CallerFilePath]`
- `[CallerLineNumber]`

这样输出的仍然是业务代码调用位置，不是封装类内部的位置。

示例：

```csharp
using System.Runtime.CompilerServices;
using FastLog;
using FastLog.Models;

public sealed class ModuleLogger
{
    private readonly Logger _logger;

    public ModuleLogger(Logger logger)
    {
        _logger = logger;
    }

    public void Info(
        string message,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        _logger.Log(LogLevel.Info, message, memberName, filePath, lineNumber);
    }

    public void Warn(
        string message,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        _logger.Log(LogLevel.Warn, message, memberName, filePath, lineNumber);
    }

    public void Error(
        string message,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        _logger.Log(LogLevel.Error, message, memberName, filePath, lineNumber);
    }
}
```

使用：

```csharp
Logger logger = new Logger(new LoggerOptions
{
    ProjectName = "PlcModule",
    BasePath = @"D:\Logs",
    SinkType = LogSinkType.File
});

ModuleLogger plcLog = new ModuleLogger(logger);

plcLog.Info("PLC模块启动");
plcLog.Warn("PLC响应变慢");
plcLog.Error("PLC连接失败");
```


格式化写入也支持保留调用方信息。常用 1-4 个格式参数可以直接传入：

```csharp
Log.Info("PLC读取耗时 {0} ms", elapsedMs);
Log.Info("条码 {0}", barcode);
```

如果需要传入动态参数数组，也可以使用数组重载：

```csharp
Log.Info("PLC读取耗时 {0} ms", new object[] { elapsedMs });
```

`MJLog` 兼容层同样提供常用 1-4 个参数的格式化重载，例如：

```csharp
logger.Info("PLC读取耗时 {0} ms", elapsedMs);
logger.Info("条码 {0}", barcode);
```

输出中的文件名、方法名和行号仍指向业务代码调用位置。旧代码中的 `params object[]` 调用也保持兼容；当调用无法匹配到固定参数重载时，会走兼容入口。由于 C# 调用方信息参数本身也是 `string`，单个字符串格式参数建议写成 `logger.Info("条码 {0}", new object[] { barcode })` 或 `logger.Info("条码 {0}", (object)barcode)`，避免被编译器解析为手动传入 `memberName`。
输出中的文件名、方法名、行号会指向：

```text
plcLog.Info("PLC模块启动");
```

这一行所在的业务代码位置。

不要这样调用：

```csharp
plcLog.Info("PLC模块启动", "手动方法名", "手动文件名", 10);
```

调用时不要手动传后三个参数，编译器才会自动填入真实调用位置。

## 5. ILogger 依赖注入

这种方式适合长期维护的分层架构。

业务层只依赖 `ILogger` 接口，不直接依赖静态 `Log`，也不直接 `new Logger`。

优点：

- 低耦合
- 方便单元测试
- 方便替换日志实现
- 符合依赖倒置原则

业务类示例：

```csharp
using FastLog;
using FastLog.Models;

public sealed class ProductionService
{
    private readonly ILogger _logger;

    public ProductionService(ILogger logger)
    {
        _logger = logger;
    }

    public void Start()
    {
        _logger.Log(LogLevel.Info, "生产流程启动", nameof(Start), "ProductionService.cs", 18);
    }
}
```

创建并注入：

```csharp
ILogger logger = new Logger(new LoggerOptions
{
    ProjectName = "Production",
    BasePath = @"D:\Logs",
    SinkType = LogSinkType.File,
    QueueFullMode = QueueFullMode.Block
});

ProductionService service = new ProductionService(logger);
service.Start();
```

`ILogger` 也可以像第 4 节一样再封装一层，用 `[CallerMemberName]`、`[CallerFilePath]`、`[CallerLineNumber]` 保留真实调用位置。

## 6. 动态切换等级和输出方式

```csharp
Log.SetLevel(LogLevel.Debug);
```

```csharp
Log.SetSink(LogSinkType.Console);
Log.Info("切换到控制台输出");

Log.SetSink(LogSinkType.File);
Log.Info("切换到文件输出");
```

可用输出类型：

```csharp
LogSinkType.File
LogSinkType.Console
LogSinkType.DebugView
LogSinkType.Udp
```

## 7. UDP 输出

```csharp
Log.Initialize(new LoggerOptions
{
    ProjectName = "贴标机",
    BasePath = @"D:\Logs",
    MinimumLevel = LogLevel.Info,
    SinkType = LogSinkType.Udp,
    UdpHost = "127.0.0.1",
    UdpPort = 9000
});
```

UDP 模式下：

- 普通日志发送到 UDP
- `Warn`、`Error`、`Fatal` 同时写入文件

## 8. 自定义 Sink

实现 `ILogSink` 可以扩展新的输出目标，例如 MES、数据库、网络服务等。

```csharp
using FastLog.Models;
using FastLog.Sinks;

public sealed class MesLogSink : ILogSink
{
    public void Write(LogMessage message)
    {
        // 将日志发送到 MES
    }

    public void Flush()
    {
    }

    public void Dispose()
    {
    }
}
```

使用：

```csharp
Log.SetSink(new MesLogSink());
Log.Info("发送到 MES 的日志");
```

## 9. 托管异常捕获

```csharp
ManagedExceptionLogger exceptionLogger = new ManagedExceptionLogger(Log.Current);
exceptionLogger.Install();
```

释放：

```csharp
exceptionLogger.Dispose();
```

说明：

- 记录 .NET 托管异常
- 不生成 Windows dump 文件
- 不替代专业崩溃转储工具

## 10. 运行时统计

```csharp
LoggerStatisticsSnapshot stat = Log.GetStatistics();

Console.WriteLine(stat.Attempted);
Console.WriteLine(stat.Enqueued);
Console.WriteLine(stat.Written);
Console.WriteLine(stat.Dropped);
Console.WriteLine(stat.SinkErrors);
Console.WriteLine(stat.QueueLength);
Console.WriteLine(stat.QueueCapacity);
```

字段说明：

- `Attempted`：尝试写入日志数量
- `Enqueued`：成功进入异步队列数量
- `Written`：实际写入 Sink 数量
- `Dropped`：丢弃日志数量
- `SinkErrors`：Sink 写入异常数量
- `QueueLength`：当前队列长度
- `QueueCapacity`：队列容量

## 11. 队列满策略

```csharp
QueueFullMode = QueueFullMode.Block
```

可选值：

- `Block`：队列满时阻塞业务线程，不丢日志，适合生产追溯
- `DropWrite`：队列满时丢弃当前新日志，业务线程不卡，适合高频调试日志
- `DropOldest`：队列满时丢弃最旧日志，保留最新现场信息，适合实时监控日志

推荐生产配置：

```csharp
QueueFullMode = QueueFullMode.Block;
QueueCapacity = 100000;
FlushBatchSize = 512;
FlushIntervalMilliseconds = 1000;
```

## 12. 文件大小滚动

```csharp
MaxFileSizeBytes = 100 * 1024 * 1024
```

滚动结果：

```text
2026-06-08.log
2026-06-08_1.log
2026-06-08_2.log
```

`MaxFileSizeBytes = 0` 表示关闭大小滚动。

## 13. 自定义日志格式和多进程文件名

FastLog 支持通过 `LoggerOptions.Formatter` 自定义内置 Sink 的输出格式。文件、控制台、DebugView、UDP 都会使用该格式器。

默认完整格式为：

```text
yyyy-MM-dd HH:mm:ss.fff [LEVEL] [FileName.cs] [TID:x] MethodName() LineNumber : Message
```

示例：

```text
2026-06-16 20:45:04.021 [DEBUG] [DAEMON] [RuntimeCheck.cs] [TID:1] Main() 9 : 程序启动
```

如果一个系统由多个进程组成，可以使用 `FileNamePrefix` 让不同进程写入不同文件，避免 `MJInspector.exe`、`Server.exe`、`Daemon.exe` 同时写同一个日期日志文件。

```csharp
Log.Initialize(new LoggerOptions
{
    ProjectName = "MJInspector",
    BasePath = AppDomain.CurrentDomain.BaseDirectory,
    SinkDirectory = @"D:\Logs\Inspection",
    FileNamePrefix = "MJInspector",
    MinimumLevel = LogLevel.Debug,
    SinkType = LogSinkType.File,
    QueueFullMode = QueueFullMode.Block,
    Formatter = message => string.Format(
        System.Globalization.CultureInfo.InvariantCulture,
        "{0:yyyy-MM-dd HH:mm:ss.fff} [{1}] [{2}] [{3}] [TID:{4}] {5}() {6} : {7}",
        message.Timestamp.LocalDateTime,
        message.Level.ToString().ToUpperInvariant(),
        message.MemberName,
        message.ThreadId,
        message.FileName,
        message.LineNumber,
        message.Message)
});
```

输出文件示例：

```text
D:\Logs\Inspection\MJInspector_2026-06-16.log
D:\Logs\Inspection\Server_2026-06-16.log
D:\Logs\Inspection\Daemon_2026-06-16.log
```

也可以通过 `LoggerConfiguration` 链式配置：

```csharp
Log.Initialize(config =>
{
    config
        .ProjectName("Server")
        .SinkDirectory(@"D:\Logs\Inspection")
        .FileNamePrefix("Server")
        .MinimumLevel(LogLevel.Debug)
        .Formatter(message => string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            "{0:yyyy-MM-dd HH:mm:ss.fff} [{1}] [{2}] [{3}] [TID:{4}] {5}() {6} : {7}",
            message.Timestamp.LocalDateTime,
            message.Level.ToString().ToUpperInvariant(),
            message.MemberName,
            message.ThreadId,
            message.FileName,
            message.LineNumber,
            message.Message))
        .WriteToFile();
});
```

`FileNamePrefix` 会自动替换文件名非法字符。`FileNamePrefix = "Server"` 时，当天第一份日志为 `Server_yyyy-MM-dd.log`，按大小滚动后为 `Server_yyyy-MM-dd_1.log`、`Server_yyyy-MM-dd_2.log`。

## 14. 性能压测工具

压测项目：

```text
CSharpLog\FastLog.Benchmark
```

编译：

```powershell
dotnet build .\CSharpLog\FastLog.Benchmark\FastLog.Benchmark.csproj
```

运行：

```text
CSharpLog\FastLog.Benchmark\bin\Debug\net472\FastLog.Benchmark.exe
```

直接运行会进入中文交互式参数设置。

命令行运行：

```powershell
dotnet run --project .\CSharpLog\FastLog.Benchmark\FastLog.Benchmark.csproj -- --threads=4 --logs=100000 --sink=File --queue=100000 --queueMode=Block --flushBatch=512 --flushMs=1000 --path=D:\Logs
```

参数说明：

- `threads`：生产线程数
- `logs`：每个线程写入日志数量
- `sink`：`File`、`Console`、`DebugView`、`Udp`
- `queue`：异步队列容量
- `queueMode`：`DropWrite`、`Block`、`DropOldest`
- `flushBatch`：写入多少条后 Flush
- `flushMs`：Flush 间隔毫秒
- `maxFileBytes`：文件大小滚动阈值，`0` 表示关闭
- `path`：日志输出目录






