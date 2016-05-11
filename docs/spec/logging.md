# Avalonia Logging

Avalonia uses [Serilog](https://github.com/serilog/serilog) for logging via
the Avalonia.Logging.Serilog assembly.

The following method should be present in your App.xaml.cs file:

```C#
private void InitializeLogging()
{
#if DEBUG
    SerilogLogger.Initialize(new LoggerConfiguration()
        .MinimumLevel.Warning()
        .WriteTo.Trace(outputTemplate: "{Area}: {Message}")
        .CreateLogger());
#endif
}
```

By default, this logging setup will write log messages with a severity of
`Warning` or higher to `System.Diagnostics.Trace`. See the [Serilog
documentation](https://github.com/serilog/serilog/wiki/Configuration-Basics)
for more information on the options here.

## Areas

Each Avalonia log message has an "Area" that can be used to filter the log to
include only the type of events that you are interested in. These are currently:

- Property
- Binding
- Visual
- Layout
- Control

To limit the log output to a specific area you can add a filter; for example
to enable verbose logging but only about layout:

```C#
SerilogLogger.Initialize(new LoggerConfiguration()
    .Filter.ByIncludingOnly(Matching.WithProperty("Area", LogArea.Layout))
    .MinimumLevel.Verbose()
    .WriteTo.Trace(outputTemplate: "{Area}: {Message}")
    .CreateLogger());
```

## Removing Serilog

If you don't want a dependency on Serilog in your application, simply remove
the reference to Avalonia.Logging.Serilog and the code that initializes it. If
you do however still want some kinda of logging, there are two steps:

- Implement `Avalonia.Logging.ILogSink`
- Assign your implementation to `Logger.Sink`
