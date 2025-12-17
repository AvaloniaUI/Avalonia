using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using Avalonia.Utilities;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Utilities
{
    [MemoryDiagnoser]
    public class StringBuilderCacheBenchmarks
    {
        private const string TestString = "Hello, World!";
        private const string LongString = "This is a longer string that will require more capacity in the StringBuilder for proper handling.";

        /// <summary>
        /// Benchmark using StringBuilderCache for short strings
        /// </summary>
        [Benchmark(Baseline = true)]
        public string StringBuilderCache_ShortString()
        {
            var sb = StringBuilderCache.Acquire();
            sb.Append(TestString);
            sb.Append(" - ");
            sb.Append(TestString);
            return StringBuilderCache.GetStringAndRelease(sb);
        }

        /// <summary>
        /// Benchmark using new StringBuilder for short strings (comparison)
        /// </summary>
        [Benchmark]
        public string NewStringBuilder_ShortString()
        {
            var sb = new StringBuilder();
            sb.Append(TestString);
            sb.Append(" - ");
            sb.Append(TestString);
            return sb.ToString();
        }

        /// <summary>
        /// Benchmark using StringBuilderCache for longer strings
        /// </summary>
        [Benchmark]
        public string StringBuilderCache_LongString()
        {
            var sb = StringBuilderCache.Acquire();
            sb.Append(LongString);
            sb.Append(" - ");
            sb.Append(LongString);
            return StringBuilderCache.GetStringAndRelease(sb);
        }

        /// <summary>
        /// Benchmark using new StringBuilder for longer strings (comparison)
        /// </summary>
        [Benchmark]
        public string NewStringBuilder_LongString()
        {
            var sb = new StringBuilder();
            sb.Append(LongString);
            sb.Append(" - ");
            sb.Append(LongString);
            return sb.ToString();
        }

        /// <summary>
        /// Benchmark StringBuilderCache with multiple appends
        /// </summary>
        [Benchmark]
        public string StringBuilderCache_MultipleAppends()
        {
            var sb = StringBuilderCache.Acquire();
            for (int i = 0; i < 10; i++)
            {
                sb.Append(i);
                sb.Append(": ");
                sb.Append(TestString);
                sb.AppendLine();
            }
            return StringBuilderCache.GetStringAndRelease(sb);
        }

        /// <summary>
        /// Benchmark new StringBuilder with multiple appends (comparison)
        /// </summary>
        [Benchmark]
        public string NewStringBuilder_MultipleAppends()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < 10; i++)
            {
                sb.Append(i);
                sb.Append(": ");
                sb.Append(TestString);
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}
