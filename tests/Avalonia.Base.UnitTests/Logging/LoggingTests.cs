using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Logging;
using Avalonia.Markup.Xaml;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Logging
{
    public class LoggingTests
    {
        [Fact]
        public void Control_Should_Not_Log_Binding_Errors_When_Detached_From_Visual_Tree()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Base.UnitTests.Logging;assembly=Avalonia.UnitTests'>
    <Panel Name='panel'>
    <Rectangle Name='rect' Fill='{Binding $parent[Window].Background}'/>
  </Panel>
</Window>";

                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var calledTimes = 0;
                using var logSink = TestLogSink.Start((l, a, s, m, d) =>
                {
                    if (l >= Avalonia.Logging.LogEventLevel.Warning)
                    {
                        calledTimes++;
                    }
                });
                var panel = window.GetControl<Panel>("panel");
                var rect = window.GetControl<Rectangle>("rect");
                window.ApplyTemplate();
                window.Presenter!.ApplyTemplate();
                panel.Children.Remove(rect);
                Assert.Equal(0, calledTimes);
            }
        }

        [Fact]
        public void Control_Should_Log_Binding_Errors_When_No_Ancestor_With_Such_Name()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Base.UnitTests.Logging;assembly=Avalonia.UnitTests'>
    <Panel>
    <Rectangle Fill='{Binding $parent[Grid].Background}'/>
  </Panel>
</Window>";
                var calledTimes = 0;
                using var logSink = TestLogSink.Start((l, a, s, m, d) =>
                {
                    if (l >= Avalonia.Logging.LogEventLevel.Warning && s is Rectangle)
                    {
                        calledTimes++;
                    }
                });
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                window.ApplyTemplate();
                window.Presenter!.ApplyTemplate();
                Assert.Equal(1, calledTimes);
            }
        }

        [Fact]
        public void LogToDelegate_Should_Use_Verbosity_Callback_Per_Area()
        {
            var messages = new List<string>();

            try
            {
                AppBuilder.Configure<Application>().LogToDelegate(messages.Add, static area => area switch
                {
                    LogArea.Binding => LogEventLevel.Information,
                    _ => LogEventLevel.Warning,
                });

                var sink = Logger.Sink!;

                Assert.True(sink.IsEnabled(LogEventLevel.Information, LogArea.Binding));
                Assert.False(sink.IsEnabled(LogEventLevel.Debug, LogArea.Binding));
                Assert.False(sink.IsEnabled(LogEventLevel.Information, LogArea.Layout));
                Assert.True(sink.IsEnabled(LogEventLevel.Warning, LogArea.Layout));

                sink.Log(LogEventLevel.Information, LogArea.Binding, null, "binding");
                sink.Log(LogEventLevel.Information, LogArea.Layout, null, "layout");
                sink.Log(LogEventLevel.Warning, LogArea.Layout, null, "warning");

                Assert.Equal(new[] { "[Binding] binding", "[Layout] warning" }, messages);
            }
            finally
            {
                Logger.Sink = null;
            }
        }

        [Fact]
        public void LogToTrace_Should_Use_Verbosity_Callback_Per_Area()
        {
            try
            {
                AppBuilder.Configure<Application>().LogToTrace(static area => area switch
                {
                    LogArea.Binding => LogEventLevel.Information,
                    _ => LogEventLevel.Warning,
                });

                var sink = Logger.Sink!;

                Assert.True(sink.IsEnabled(LogEventLevel.Information, LogArea.Binding));
                Assert.False(sink.IsEnabled(LogEventLevel.Information, LogArea.Layout));
                Assert.True(sink.IsEnabled(LogEventLevel.Warning, LogArea.Layout));
            }
            finally
            {
                Logger.Sink = null;
            }
        }
    }


}
