using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Avalonia;
using Avalonia.Logging;
using Avalonia.Utilities;

namespace AotSandbox;

public static class Program
{
    public static void Main(string[] args)
    {
        //Logger.Sink = new ConsoleLog(LogEventLevel.Warning);
        AppBuilder.Configure<App>()
            .With(new X11PlatformOptions()
            {
                UseDeferredRendering = false,
                UseCompositor = false
            })
            .UseSkia().UseX11().StartWithClassicDesktopLifetime(args);
        
    }
    public class ConsoleLog : ILogSink
    {
        private readonly LogEventLevel _level;
        private readonly IList<string>? _areas;

        public ConsoleLog(
            LogEventLevel minimumLevel,
            IList<string>? areas = null)
        {
            _level = minimumLevel;
            _areas = areas?.Count > 0 ? areas : null;
        }

        public bool IsEnabled(LogEventLevel level, string area)
        {
            return level >= _level && (_areas?.Contains(area) ?? true);
        }

        public void Log(LogEventLevel level, string area, object? source, string messageTemplate)
        {
            if (IsEnabled(level, area))
            {
                Console.WriteLine(Format<object, object, object>(area, messageTemplate, source, null));
            }
        }

        public void Log(LogEventLevel level, string area, object? source, string messageTemplate, params object?[] propertyValues)
        {
            if (IsEnabled(level, area))
            {
                Console.WriteLine(Format(area, messageTemplate, source, propertyValues));
            }
        }

        private static string Format<T0, T1, T2>(
            string area,
            string template,
            object? source,
            object?[]? values)
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
                        result.Append(values?[i++]);
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
            
            return result.ToString();
        }

        private static string Format(
            string area,
            string template,
            object? source,
            object?[] v)
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

            return result.ToString();
        }
    }
}