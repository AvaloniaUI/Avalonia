using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Input.TextInput;
using Avalonia.Native.Interop;

namespace Avalonia.Native;

/// <summary>
/// Thin managed adapter over the AppKit spell-check implementation in Avalonia.Native.
/// </summary>
internal sealed class MacOSSpellCheckProvider : ISpellCheckProvider
{
    private static MacOSSpellCheckProvider? s_shared;
    private readonly IAvnSpellCheckProvider _native;

    private MacOSSpellCheckProvider(IAvnSpellCheckProvider native)
    {
        _native = native;
    }

    // AppKit has one shared spell checker, so one provider serves every window for the process lifetime.
    public static MacOSSpellCheckProvider GetShared(IAvaloniaNativeFactory factory) =>
        s_shared ??= new MacOSSpellCheckProvider(factory.CreateSpellCheckProvider());

    public IReadOnlyList<CultureInfo> SupportedCultures
    {
        get
        {
            using var values = _native.SupportedCultures;
            var cultures = new List<CultureInfo>((int)values.Count);

            foreach (var value in values.ToStringArray())
            {
                try
                {
                    cultures.Add(CultureInfo.GetCultureInfo(value.Replace('_', '-')));
                }
                catch (CultureNotFoundException)
                {
                }
            }

            return cultures;
        }
    }

    public ISpellCheckContext? CreateContext(CultureInfo culture)
    {
        using var value = culture.Name.ToAvnString();
        var context = _native.TryCreateContext(value);
        return context is null ? null : new NativeContext(context);
    }

    private sealed class NativeContext : SpellCheckContextBase
    {
        private readonly IAvnSpellCheckContext _native;

        public NativeContext(IAvnSpellCheckContext native)
        {
            _native = native;
        }

        protected override ValueTask<IReadOnlyList<ISpellCheckResult>> CheckCoreAsync(
            ReadOnlyMemory<char> textMemory,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var text = textMemory.ToString();
            using var value = text.ToAvnString();
            using var ranges = _native.Check(value);
            var count = ranges.Count;

            if (count == 0)
            {
                return new ValueTask<IReadOnlyList<ISpellCheckResult>>(Array.Empty<ISpellCheckResult>());
            }

            var results = new List<ISpellCheckResult>((int)count);

            for (uint i = 0; i < count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var range = ranges.Get(i);

                if (range.Start >= 0 && range.Length > 0 && range.Start + range.Length <= text.Length)
                {
                    results.Add(new NativeResult(this, text, range.Start, range.Length));
                }
            }

            return new ValueTask<IReadOnlyList<ISpellCheckResult>>(results);
        }

        public ValueTask<IReadOnlyList<string>> SuggestCoreAsync(
            string text,
            int start,
            int length,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var value = text.ToAvnString();
            using var suggestions = _native.GetSuggestions(value, start, length);
            return new ValueTask<IReadOnlyList<string>>(suggestions.ToStringArray());
        }

        protected override void DisposeCore()
        {
            _native.Dispose();
        }
    }

    // Keeps the checked text so suggestions can use the surrounding words.
    private sealed class NativeResult : SpellCheckResultBase
    {
        private readonly NativeContext _context;
        private readonly string _text;

        public NativeResult(NativeContext context, string text, int start, int length)
            : base(context, start, length)
        {
            _context = context;
            _text = text;
        }

        protected override ValueTask<IReadOnlyList<string>> SuggestCoreAsync(CancellationToken cancellationToken) =>
            _context.SuggestCoreAsync(_text, Start, Length, cancellationToken);
    }
}
