#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Rendering
{
    /// <summary>
    /// Benchmarks for Avalonia.Android hot path patterns.
    /// Run with: dotnet run -c Release -- --filter *AndroidRenderingBenchmarks*
    /// </summary>
    [MemoryDiagnoser]
    public class AndroidRenderingBenchmarks
    {
        private const int FrameCount = 1000;

        // DispatchDraw: new Paint() every frame vs cached (TopLevelImpl.cs)

        [Benchmark]
        public object? Current_DispatchDraw_NewObjectPerFrame()
        {
            object? last = null;
            for (int i = 0; i < FrameCount; i++)
            {
                var obj = new DispatchDrawState();
                obj.Color = 0;
                obj.Mode = 1;
                last = obj;
            }
            return last;
        }

        [Benchmark]
        public object? Optimized_DispatchDraw_CachedObject()
        {
            DispatchDrawState? cached = null;
            for (int i = 0; i < FrameCount; i++)
            {
                if (cached == null)
                {
                    cached = new DispatchDrawState();
                    cached.Color = 0;
                    cached.Mode = 1;
                }
            }
            return cached;
        }

        private sealed class DispatchDrawState
        {
            public int Color;
            public int Mode;
        }

        // Dispatcher.Signal(): lock vs Interlocked (AndroidDispatcherImpl.cs)

        private readonly object _lock = new();

        [Benchmark]
        public int Current_DispatcherSignal_Lock()
        {
            int signaled = 0;
            bool flag = false;
            for (int i = 0; i < FrameCount; i++)
            {
                lock (_lock)
                {
                    if (!flag)
                    {
                        flag = true;
                        signaled++;
                    }
                }
                lock (_lock)
                {
                    flag = false;
                }
            }
            return signaled;
        }

        [Benchmark]
        public int Optimized_DispatcherSignal_Interlocked()
        {
            int signaled = 0;
            int flag = 0;
            for (int i = 0; i < FrameCount; i++)
            {
                if (Interlocked.CompareExchange(ref flag, 1, 0) == 0)
                {
                    signaled++;
                }
                Interlocked.Exchange(ref flag, 0);
            }
            return signaled;
        }

        // TextInput timestamp: DateTime.Now vs monotonic clock (TopLevelImpl.cs)

        [Benchmark]
        public ulong Current_TextInput_DateTimeNowTicks()
        {
            ulong result = 0;
            for (int i = 0; i < FrameCount; i++)
            {
                result = (ulong)DateTime.Now.Ticks;
            }
            return result;
        }

        [Benchmark]
        public long Optimized_TextInput_StopwatchTimestamp()
        {
            long result = 0;
            for (int i = 0; i < FrameCount; i++)
            {
                result = System.Diagnostics.Stopwatch.GetTimestamp();
            }
            return result;
        }

        // Accessibility action: LINQ vs loop (AvaloniaAccessHelper.cs)

        private readonly List<int> _providers = new() { 1, 2, 3 };

        [Benchmark]
        public bool Current_AccessAction_Linq()
        {
            bool result = false;
            for (int i = 0; i < FrameCount; i++)
            {
                result = _providers
                    .Select(x => x > 1)
                    .Aggregate(false, (a, b) => a | b);
            }
            return result;
        }

        [Benchmark]
        public bool Optimized_AccessAction_Loop()
        {
            bool result = false;
            for (int i = 0; i < FrameCount; i++)
            {
                foreach (var p in _providers)
                {
                    result |= p > 1;
                }
            }
            return result;
        }

        // Keyboard char.ToString() vs cached string (AndroidKeyboardEventsHelper.cs)

        private static readonly string[] s_asciiStringCache = CreateAsciiCache();

        private static string[] CreateAsciiCache()
        {
            var cache = new string[128];
            for (int i = 0; i < 128; i++)
            {
                cache[i] = ((char)i).ToString();
            }
            return cache;
        }

        [Benchmark]
        public string? Current_KeySymbol_CharToString()
        {
            string? last = null;
            for (int i = 0; i < FrameCount; i++)
            {
                char c = (char)(32 + (i % 95));
                last = c.ToString();
            }
            return last;
        }

        [Benchmark]
        public string? Optimized_KeySymbol_CachedString()
        {
            string? last = null;
            for (int i = 0; i < FrameCount; i++)
            {
                int code = 32 + (i % 95);
                last = s_asciiStringCache[code];
            }
            return last;
        }

    }
}
