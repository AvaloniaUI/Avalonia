using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Avalonia.Utilities;

namespace Avalonia.Logging
{
    internal class StringLogSink : ILogSink
    {
        private readonly Action<string> _logger;
        private readonly LogEventLevel _level;
        private readonly IList<string>? _areas;
        private readonly Func<string, LogEventLevel>? _areaVerbosity;

        public StringLogSink(
            Action<string> logger,
            LogEventLevel minimumLevel,
            IList<string>? areas = null)
        {
            _logger = logger;
            _level = minimumLevel;
            _areas = areas?.Count > 0 ? areas : null;
        }

        public StringLogSink(
            Action<string> logger,
            Func<string, LogEventLevel> areaVerbosity)
        {
            _logger = logger;
            _areaVerbosity = areaVerbosity;
        }

        public bool IsEnabled(LogEventLevel level, string area)
        {
            if (_areaVerbosity is not null)
                return level >= _areaVerbosity(area);

            return level >= _level && (_areas?.Contains(area) ?? true);
        }

        public void Log(LogEventLevel level, string area, object? source, string messageTemplate)
        {
            if (IsEnabled(level, area))
            {
                _logger(Format<object, object, object>(area, messageTemplate, source, null));
            }
        }

        public void Log(LogEventLevel level, string area, object? source, string messageTemplate, params object?[] propertyValues)
        {
            if (IsEnabled(level, area))
            {
                _logger(Format(area, messageTemplate, source, propertyValues));
            }
        }

        private static string Format<T0, T1, T2>(
            string area,
            string template,
            object? source,
            object?[]? values)
        {
            var result = StringBuilderCache.Acquire(template.Length);
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

            FormatSource(source, result);
            return StringBuilderCache.GetStringAndRelease(result);
        }

        private static string Format(
            string area,
            string template,
            object? source,
            object?[] v)
        {
            var result = StringBuilderCache.Acquire(template.Length);
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

            FormatSource(source, result);
            return StringBuilderCache.GetStringAndRelease(result);
        }

        private static void FormatSource(object? source, StringBuilder result)
        {
            if (source is null)
                return;

            result.Append(" (");
            result.Append(source.GetType().Name);
            result.Append(" #");

            if (source is StyledElement se && se.Name is not null)
                result.Append(se.Name);
            else
                result.Append(source.GetHashCode());

            result.Append(')');
        }
    }
}
