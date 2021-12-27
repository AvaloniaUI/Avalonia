using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Avalonia.Utilities;

namespace Avalonia.Logging
{
    public class TraceLogSink : ILogSink
    {
        private readonly LogEventLevel _level;
        private readonly IList<string>? _areas;

        public TraceLogSink(
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
                Trace.WriteLine(Format<object, object, object>(area, messageTemplate, source));
            }
        }

        public void Log<T0>(LogEventLevel level, string area, object? source, string messageTemplate, T0 propertyValue0)
        {
            if (IsEnabled(level, area))
            {
                Trace.WriteLine(Format<T0, object, object>(area, messageTemplate, source, propertyValue0));
            }
        }

        public void Log<T0, T1>(LogEventLevel level, string area, object? source, string messageTemplate, T0 propertyValue0, T1 propertyValue1)
        {
            if (IsEnabled(level, area))
            {
                Trace.WriteLine(Format<T0, T1, object>(area, messageTemplate, source, propertyValue0, propertyValue1));
            }
        }

        public void Log<T0, T1, T2>(LogEventLevel level, string area, object? source, string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2)
        {
            if (IsEnabled(level, area))
            {
                Trace.WriteLine(Format(area, messageTemplate, source, propertyValue0, propertyValue1, propertyValue2));
            }
        }

        public void Log(LogEventLevel level, string area, object? source, string messageTemplate, params object?[] propertyValues)
        {
            if (IsEnabled(level, area))
            {
                Trace.WriteLine(Format(area, messageTemplate, source, propertyValues));
            }
        }

        private static string Format<T0, T1, T2>(
            string area,
            string template,
            object? source,
            T0? v0 = default,
            T1? v1 = default,
            T2? v2 = default)
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
    }
}
