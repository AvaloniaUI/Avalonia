using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Logging;
using Avalonia.Markup.Xaml;
using Avalonia.UnitTests;
using Avalonia.Utilities;
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
                using var logSink = new StubLogSink(LogEventLevel.Warning);
                var panel = window.FindControl<Panel>("panel");
                var rect = window.FindControl<Rectangle>("rect");
                window.ApplyTemplate();
                window.Presenter.ApplyTemplate();
                panel.Children.Remove(rect);
                Assert.Equal(0, logSink.Results.Count);
            }
        }
    } 

    class StubLogSink : ILogSink, IDisposable
    {
        LogEventLevel _level;
        public StubLogSink(LogEventLevel level)
        {
            _level = level;
            Logger.Sink = this;
        }
        public void Dispose()
        {
            Logger.Sink = null;
        }
        public List<string> Results { get; set; } = new List<string>();

        public bool IsEnabled(LogEventLevel level, string area)
        {
            return true;
        }

        public void Log(LogEventLevel level, string area, object source, string messageTemplate)
        {
            if (level >= _level)
            {
                Results.Add(Format<object, object, object>(area, messageTemplate, source));
            }
        }

        public void Log<T0>(LogEventLevel level, string area, object source, string messageTemplate, T0 propertyValue0)
        {
            if (level >= _level)
            {
                Results.Add(Format<T0, object, object>(area, messageTemplate, source, propertyValue0));
            }
        }

        public void Log<T0, T1>(LogEventLevel level, string area, object source, string messageTemplate, T0 propertyValue0, T1 propertyValue1)
        {
            if (level >= _level)
            {
                Results.Add(Format<T0, T1, object>(area, messageTemplate, source, propertyValue0, propertyValue1));
            }
        }

        public void Log<T0, T1, T2>(LogEventLevel level, string area, object source, string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2)
        {
            if (level >= _level)
            {
                Results.Add(Format(area, messageTemplate, source, propertyValue0, propertyValue1, propertyValue2));
            }
        }

        public void Log(LogEventLevel level, string area, object source, string messageTemplate, params object[] propertyValues)
        {
            if (level >= _level)
            {
                Results.Add(Format(area, messageTemplate, source, propertyValues));
            }
        }
        #region Copy-Pasta
        private static string Format<T0, T1, T2>(
           string area,
           string template,
           object source,
           T0 v0 = default,
           T1 v1 = default,
           T2 v2 = default)
        {
            var result = new StringBuilder(template.Length);
            var r = new CharacterReader(template.AsSpan());
            var i = 0;

            result.Append('[');
            result.Append(area);
            result.Append("] ");

            while (!r.End)
            {
                var c = r.Take();

                if (c != '{')
                {
                    result.Append(c);
                }
                else
                {
                    if (r.Peek != '{')
                    {
                        result.Append('\'');
                        result.Append(i++ switch
                        {
                            0 => v0,
                            1 => v1,
                            2 => v2,
                            _ => null
                        });
                        result.Append('\'');
                        r.TakeUntil('}');
                        r.Take();
                    }
                    else
                    {
                        result.Append('{');
                        r.Take();
                    }
                }
            }

            if (source is object)
            {
                result.Append(" (");
                result.Append(source.GetType().Name);
                result.Append(" #");
                result.Append(source.GetHashCode());
                result.Append(')');
            }

            return result.ToString();
        }

        private static string Format(
            string area,
            string template,
            object source,
            object[] v)
        {
            var result = new StringBuilder(template.Length);
            var r = new CharacterReader(template.AsSpan());
            var i = 0;

            result.Append('[');
            result.Append(area);
            result.Append(']');

            while (!r.End)
            {
                var c = r.Take();

                if (c != '{')
                {
                    result.Append(c);
                }
                else
                {
                    if (r.Peek != '{')
                    {
                        result.Append('\'');
                        result.Append(i < v.Length ? v[i++] : null);
                        result.Append('\'');
                        r.TakeUntil('}');
                        r.Take();
                    }
                    else
                    {
                        result.Append('{');
                        r.Take();
                    }
                }
            }

            if (source is object)
            {
                result.Append('(');
                result.Append(source.GetType().Name);
                result.Append(" #");
                result.Append(source.GetHashCode());
                result.Append(')');
            }

            return result.ToString();
        }
        #endregion
    }
}
