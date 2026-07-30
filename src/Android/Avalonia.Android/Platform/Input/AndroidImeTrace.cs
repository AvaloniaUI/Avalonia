using System;
using Avalonia.Logging;
using AndroidTrace = global::Android.OS.Trace;

namespace Avalonia.Android.Platform.Input
{
    /// <summary>
    /// Tracing for the Android IME event flow. Each input-method transaction emits a compact structured
    /// line to the Avalonia logger under <see cref="LogArea.TextInput"/>; register a log sink that accepts
    /// that area (for example an on-screen or logcat sink) to observe the flow. When <see cref="IsEnabled"/>
    /// is additionally set, <see cref="BeginSection"/> also emits a matching Perfetto/systrace section for
    /// timeline profiling.
    /// </summary>
    /// <remarks>
    /// Logging follows the standard Avalonia pattern: the <see cref="Logger"/> lookup returns null and the
    /// call is a cheap no-op unless a sink is listening for <see cref="LogArea.TextInput"/>, so the
    /// <see cref="Event"/> overloads (a message template plus up to six typed values, mirroring
    /// <see cref="ParametrizedLogger"/>) never format or box at the call site when nothing is capturing.
    /// <see cref="IsEnabled"/> gates only the extra systrace overhead and is off by default.
    /// </remarks>
    internal static class AndroidImeTrace
    {
        /// <summary>
        /// Enables Perfetto/systrace sections from <see cref="BeginSection"/>. Off by default; logging is
        /// controlled independently by whether a sink accepts <see cref="LogArea.TextInput"/>.
        /// </summary>
        public static bool IsEnabled;

        public static void Event(object? source, string template)
            => Logger.TryGet(LogEventLevel.Debug, LogArea.TextInput)?.Log(source, template);

        public static void Event<T0>(object? source, string template, T0 v0)
            => Logger.TryGet(LogEventLevel.Debug, LogArea.TextInput)?.Log(source, template, v0);

        public static void Event<T0, T1>(object? source, string template, T0 v0, T1 v1)
            => Logger.TryGet(LogEventLevel.Debug, LogArea.TextInput)?.Log(source, template, v0, v1);

        public static void Event<T0, T1, T2>(object? source, string template, T0 v0, T1 v1, T2 v2)
            => Logger.TryGet(LogEventLevel.Debug, LogArea.TextInput)?.Log(source, template, v0, v1, v2);

        public static void Event<T0, T1, T2, T3>(object? source, string template, T0 v0, T1 v1, T2 v2, T3 v3)
            => Logger.TryGet(LogEventLevel.Debug, LogArea.TextInput)?.Log(source, template, v0, v1, v2, v3);

        public static void Event<T0, T1, T2, T3, T4>(object? source, string template, T0 v0, T1 v1, T2 v2, T3 v3, T4 v4)
            => Logger.TryGet(LogEventLevel.Debug, LogArea.TextInput)?.Log(source, template, v0, v1, v2, v3, v4);

        public static void Event<T0, T1, T2, T3, T4, T5>(object? source, string template, T0 v0, T1 v1, T2 v2, T3 v3, T4 v4, T5 v5)
            => Logger.TryGet(LogEventLevel.Debug, LogArea.TextInput)?.Log(source, template, v0, v1, v2, v3, v4, v5);

        /// <summary>
        /// Begins a systrace/Perfetto section that ends when the returned value is disposed. Returns a no-op
        /// section unless <see cref="IsEnabled"/> is set, so a hot path can always wrap in
        /// <c>using var _ = AndroidImeTrace.BeginSection(...)</c>.
        /// </summary>
        public static Section BeginSection(string name)
        {
            if (!IsEnabled)
                return default;

            AndroidTrace.BeginSection(name);
            return new Section(true);
        }

        /// <summary>
        /// A systrace section scope. Ends the section on <see cref="Dispose"/>; a default value is a no-op.
        /// </summary>
        public readonly struct Section : IDisposable
        {
            private readonly bool _active;

            internal Section(bool active) => _active = active;

            public void Dispose()
            {
                if (_active)
                    AndroidTrace.EndSection();
            }
        }
    }
}
