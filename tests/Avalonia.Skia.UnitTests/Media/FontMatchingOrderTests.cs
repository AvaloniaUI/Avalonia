#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Platform;
using Avalonia.UnitTests;
using SkiaSharp;
using Xunit;

namespace Avalonia.Skia.UnitTests.Media
{
    /// <summary>
    /// A system font collection answers a request the same way whatever it resolved before, and asks
    /// the platform once per distinct request.
    /// </summary>
    /// <remarks>
    /// Each request is first resolved in a fresh collection, which gives its expected result. Then
    /// every ordered pair of requests, and seeded shuffles of all of them, are replayed against one
    /// collection each, and every result must equal the expected one: the key the glyph typeface
    /// reports, its simulations, whether it is drawn slanted or emboldened, and the real face it
    /// draws. Replaying a sequence a second time must not reach the platform at all.
    /// </remarks>
    public class FontMatchingOrderTests
    {
        private const int LatinCapitalA = 'A';

        // U+1F600, covered only by the colour font of the model.
        private const int GrinningFace = 0x1F600;

        // U+4E2D, covered only by the CJK font of the model.
        private const int CjkCodepoint = 0x4E2D;

        private const int ShuffleCount = 8;

        private static readonly FontCollectionKey[] s_keys = CreateKeys();

        public static IEnumerable<object[]> ModelFamilies() => new[]
        {
            DirectWriteLikeFontManagerImpl.InterFamily,
            DirectWriteLikeFontManagerImpl.NotoMonoFamily,
            DirectWriteLikeFontManagerImpl.SourceSerifItalicFamily,
            DirectWriteLikeFontManagerImpl.EmojiFamily,
            DirectWriteLikeFontManagerImpl.CjkFamily
        }.Select(x => new object[] { x });

        [Theory]
        [MemberData(nameof(ModelFamilies))]
        public void Family_Matches_Should_Not_Depend_On_Earlier_Requests(string familyName)
        {
            using var platform = new DirectWriteLikeFontManagerImpl();
            var counting = new CountingFontManagerImpl(platform);

            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: counting)))
            {
                var family = platform.GetFamily(familyName);
                var harness = new Harness(counting,
                    x => DescribeModelFace(platform.GetFace(x)),
                    x => platform.GetFamily(x).HasUprightFace);

                harness.Verify(CreateRequests(familyName, FallbackCodepoint(family)));
            }
        }

        public static IEnumerable<object[]> InstalledFamilies() => new[]
        {
            // A regular and a bold face only.
            "Tahoma",
            // A single regular colour face.
            "Segoe UI Emoji",
            // Real faces from Light to Black, upright and italic.
            "Arial"
        }.Select(x => new object[] { x });

        [Win32Theory("Windows specific fonts")]
        [MemberData(nameof(InstalledFamilies))]
        public void Installed_Family_Matches_Should_Not_Depend_On_Earlier_Requests(string familyName)
        {
            var platform = new FontManagerImpl();

            if (!platform.GetInstalledFontFamilyNames().Contains(familyName, StringComparer.OrdinalIgnoreCase))
            {
                Assert.Skip($"{familyName} is not installed.");
            }

            var counting = new CountingFontManagerImpl(platform);

            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface.With(fontManagerImpl: counting)))
            {
                var harness = new Harness(counting, DescribeInstalledFace,
                    x => platform.TryGetFamilyTypefaces(x, out var typefaces) &&
                         typefaces.Any(t => t.Style == FontStyle.Normal));

                var isColour = string.Equals(familyName, "Segoe UI Emoji", StringComparison.OrdinalIgnoreCase);

                harness.Verify(CreateRequests(familyName, isColour ? GrinningFace : LatinCapitalA,
                    fallbackFamily: isColour ? null : familyName));
            }
        }

        private static int FallbackCodepoint(DirectWriteLikeFontManagerImpl.Family family) => family.Name switch
        {
            DirectWriteLikeFontManagerImpl.EmojiFamily => GrinningFace,
            DirectWriteLikeFontManagerImpl.CjkFamily => CjkCodepoint,
            _ => LatinCapitalA
        };

        private static IReadOnlyList<Request> CreateRequests(string familyName, int fallbackCodepoint)
        {
            // A Latin letter is requested through its family, as a text run of that family asks for
            // it; the codepoints only one font covers come from plain fallback.
            var fallbackFamily = fallbackCodepoint == LatinCapitalA ? familyName : null;

            return CreateRequests(familyName, fallbackCodepoint, fallbackFamily);
        }

        private static IReadOnlyList<Request> CreateRequests(string familyName, int fallbackCodepoint,
            string? fallbackFamily)
        {
            var requests = new List<Request>();

            foreach (var key in s_keys)
            {
                requests.Add(new Request(familyName, key, null));
            }

            foreach (var key in s_keys)
            {
                requests.Add(new Request(fallbackFamily, key, fallbackCodepoint));
            }

            return requests;
        }

        private static FontCollectionKey[] CreateKeys()
        {
            var keys = new List<FontCollectionKey>();

            foreach (var weight in new[] { FontWeight.Normal, FontWeight.SemiBold, FontWeight.Bold, FontWeight.ExtraBold })
            {
                foreach (var style in new[] { FontStyle.Normal, FontStyle.Italic, FontStyle.Oblique })
                {
                    keys.Add(new FontCollectionKey(style, weight, FontStretch.Normal));
                }
            }

            keys.Add(new FontCollectionKey(FontStyle.Normal, FontWeight.Normal, FontStretch.Condensed));
            keys.Add(new FontCollectionKey(FontStyle.Italic, FontWeight.Bold, FontStretch.Expanded));

            return keys.ToArray();
        }

        private static FaceDescription DescribeModelFace(DirectWriteLikeFontManagerImpl.Face face) =>
            new(face.Name, face.Style != FontStyle.Normal, face.IsColor);

        private static FaceDescription DescribeInstalledFace(SKTypeface skTypeface)
        {
            // What the platform answered with, simulations applied inside the typeface included.
            var fontStyle = skTypeface.FontStyle;

            return new FaceDescription(
                $"{skTypeface.FamilyName} {fontStyle.Weight} {fontStyle.Width} {fontStyle.Slant}",
                fontStyle.Slant != SKFontStyleSlant.Upright, SkiaTypeface.IsColorFont(skTypeface));
        }

        /// <summary>A family match when <paramref name="Codepoint"/> is <c>null</c>, otherwise a character
        /// fallback for it, optionally preferring <paramref name="FamilyName"/>.</summary>
        private readonly record struct Request(string? FamilyName, FontCollectionKey Key, int? Codepoint)
        {
            public string Kind => Codepoint is null ? "family match" : "character fallback";

            public override string ToString()
            {
                var key = $"{Key.Style} {Key.Weight} {Key.Stretch}";

                return Codepoint is { } codepoint ?
                    $"U+{codepoint:X4} via {FamilyName ?? "fallback"} {key}" :
                    $"{FamilyName} {key}";
            }
        }

        /// <param name="Name">Identifies the real face drawn.</param>
        /// <param name="IsSlanted">Whether the face is slanted as the platform returned it.</param>
        /// <param name="IsColor">Whether the face draws its glyphs from a colour table.</param>
        private readonly record struct FaceDescription(string Name, bool IsSlanted, bool IsColor);

        /// <param name="ReportedKey">The style, weight and stretch the glyph typeface reports.</param>
        /// <param name="Simulations">The simulations the glyph typeface reports.</param>
        /// <param name="IsDrawnSlanted">Whether the glyphs end up slanted, by the face, the platform or Avalonia.</param>
        /// <param name="IsDrawnEmboldened">Whether the glyphs end up emboldened by the platform or Avalonia.</param>
        /// <param name="Face">The real face drawn.</param>
        private readonly record struct Outcome(FontCollectionKey ReportedKey, FontSimulations Simulations,
            bool IsDrawnSlanted, bool IsDrawnEmboldened, string Face)
        {
            public override string ToString() =>
                $"{Face} as {ReportedKey.Style} {ReportedKey.Weight} {ReportedKey.Stretch} [{Simulations}]" +
                (IsDrawnSlanted ? " slanted" : "") + (IsDrawnEmboldened ? " emboldened" : "");
        }

        private sealed class Harness
        {
            private readonly CountingFontManagerImpl _platform;
            private readonly Func<SKTypeface, FaceDescription> _describe;
            private readonly Func<string, bool> _hasUprightFace;

            public Harness(CountingFontManagerImpl platform, Func<SKTypeface, FaceDescription> describe,
                Func<string, bool> hasUprightFace)
            {
                _platform = platform;
                _describe = describe;
                _hasUprightFace = hasUprightFace;
            }

            public void Verify(IReadOnlyList<Request> requests)
            {
                var failures = new Violations();
                var expected = new Dictionary<Request, Outcome>();

                foreach (var request in requests)
                {
                    using var collection = new InspectableSystemFontCollection(_platform);

                    if (TryResolve(collection, request, failures, out var outcome))
                    {
                        expected[request] = outcome;
                    }
                }

                var sequences = new List<Request[]>();

                foreach (var first in requests)
                {
                    foreach (var second in requests)
                    {
                        if (first != second)
                        {
                            sequences.Add(new[] { first, second });
                        }
                    }
                }

                for (var seed = 1; seed <= ShuffleCount; seed++)
                {
                    var random = new Random(seed);

                    sequences.Add(requests.OrderBy(_ => random.Next()).ToArray());
                }

                foreach (var sequence in sequences)
                {
                    Replay(sequence, expected, failures);
                }

                Assert.True(failures.Count == 0, failures.Report(sequences.Count));
            }

            private void Replay(Request[] sequence, Dictionary<Request, Outcome> expected, Violations failures)
            {
                using var collection = new InspectableSystemFontCollection(_platform);

                var sequenceName = "[" + string.Join(", ", sequence) + "]";
                var resolved = new List<(Request Request, GlyphTypeface GlyphTypeface)>();

                _platform.Reset();

                foreach (var request in sequence)
                {
                    if (!TryResolve(collection, request, out var glyphTypeface, failures))
                    {
                        continue;
                    }

                    resolved.Add((request, glyphTypeface));

                    if (!TryDescribe(collection, request, glyphTypeface, failures, out var outcome) ||
                        !expected.TryGetValue(request, out var baseline))
                    {
                        continue;
                    }

                    if (outcome != baseline)
                    {
                        var category = sequence.Length == 2 ?
                            $"order: {request.Kind} after {sequence[0].Kind} differs from its fresh result" :
                            "order: a request in a shuffled sequence differs from its fresh result";

                        failures.Add(category, $"{sequenceName}: {request} gave {outcome}, alone it gives {baseline}");
                    }
                }

                foreach (var repeated in _platform.Repeated())
                {
                    failures.Add("platform: asked more than once for one request",
                        $"{sequenceName}: asked {repeated.Value} times for {repeated.Key}");
                }

                foreach (var call in _platform.Calls().Where(x => x.StartsWith(CountingFontManagerImpl.StreamCall)))
                {
                    failures.Add("platform: a synthetic face copied the font file", $"{sequenceName}: {call}");
                }

                _platform.Reset();

                foreach (var request in sequence)
                {
                    TryResolve(collection, request, failures, out _);
                }

                foreach (var call in _platform.Calls())
                {
                    failures.Add("platform: replaying a sequence asked again", $"{sequenceName}: {call}");
                }

                // A platform can hand out one typeface object for every match of the same face, so
                // releasing a duplicate answer must not release the face an earlier result draws with.
                foreach (var (request, glyphTypeface) in resolved)
                {
                    if (((SkiaTypeface)glyphTypeface.PlatformTypeface).SKTypeface.Handle == IntPtr.Zero)
                    {
                        failures.Add("invariant: a resolved face was released while the collection holds it",
                            $"{sequenceName}: {request}");
                    }
                }
            }

            private bool TryResolve(InspectableSystemFontCollection collection, Request request,
                Violations failures, out Outcome outcome)
            {
                outcome = default;

                return TryResolve(collection, request, out var glyphTypeface, failures) &&
                       TryDescribe(collection, request, glyphTypeface, failures, out outcome);
            }

            private static bool TryResolve(InspectableSystemFontCollection collection, Request request,
                [NotNullWhen(true)] out GlyphTypeface? glyphTypeface, Violations failures)
            {
                var key = request.Key;

                glyphTypeface = null;

                if (request.Codepoint is { } codepoint)
                {
                    if (!collection.TryMatchCharacter(codepoint, key.Style, key.Weight, key.Stretch,
                            request.FamilyName, null, out var match) ||
                        !collection.TryGetGlyphTypeface(match.FontFamily.Name, match.Style, match.Weight,
                            match.Stretch, out glyphTypeface))
                    {
                        failures.Add("no match", request.ToString());

                        return false;
                    }
                }
                else if (!collection.TryGetGlyphTypeface(request.FamilyName!, key.Style, key.Weight, key.Stretch,
                             out glyphTypeface))
                {
                    failures.Add("no match", request.ToString());

                    return false;
                }

                return true;
            }

            private bool TryDescribe(InspectableSystemFontCollection collection, Request request,
                GlyphTypeface glyphTypeface, Violations failures, out Outcome outcome)
            {
                var skiaTypeface = (SkiaTypeface)glyphTypeface.PlatformTypeface;
                var face = _describe(skiaTypeface.SKTypeface);

                // The platform typeface carries the simulations applied inside it as well as the
                // ones applied when it is drawn.
                var simulations = glyphTypeface.FontSimulations | skiaTypeface.FontSimulations;

                outcome = new Outcome(
                    new FontCollectionKey(glyphTypeface.Style, glyphTypeface.Weight, glyphTypeface.Stretch),
                    glyphTypeface.FontSimulations,
                    face.IsSlanted || (simulations & FontSimulations.Oblique) != 0,
                    (simulations & FontSimulations.Bold) != 0,
                    face.Name);

                CheckInvariants(collection, request, glyphTypeface, face, outcome, failures);

                return true;
            }

            private void CheckInvariants(InspectableSystemFontCollection collection, Request request,
                GlyphTypeface glyphTypeface, FaceDescription face, Outcome outcome, Violations failures)
            {
                if (request.Key.Style == FontStyle.Normal && outcome.IsDrawnSlanted &&
                    _hasUprightFace(glyphTypeface.FamilyName))
                {
                    failures.Add("invariant: an upright request is drawn slanted", $"{request}: {outcome}");
                }

                // A simulation the platform applies inside the typeface reaches every glyph, colour
                // glyphs included, while one applied when drawing is left to the renderer.
                if (face.IsColor && ((SkiaTypeface)glyphTypeface.PlatformTypeface).PlatformSimulations !=
                    FontSimulations.None)
                {
                    failures.Add("invariant: a colour face is simulated by the platform", $"{request}: {outcome}");
                }

                var cachedKeys = collection.GetCachedKeys(glyphTypeface);

                if (outcome.ReportedKey != Plain(request.Key) && !cachedKeys.Contains(outcome.ReportedKey))
                {
                    failures.Add("invariant: the reported key is neither the requested key nor a key it is cached under",
                        $"{request}: {outcome}, cached under " +
                        string.Join(", ", cachedKeys.Select(x => $"{x.Style} {x.Weight} {x.Stretch}")));
                }
            }

            private static FontCollectionKey Plain(FontCollectionKey key) => new(key.Style, key.Weight, key.Stretch);
        }

        /// <summary>Violations grouped by what went wrong, each with a few distinct examples.</summary>
        private sealed class Violations
        {
            private const int ExamplesPerCategory = 5;

            private readonly SortedDictionary<string, (int Count, List<string> Examples)> _byCategory =
                new(StringComparer.Ordinal);

            public int Count { get; private set; }

            public void Add(string category, string example)
            {
                Count++;

                if (!_byCategory.TryGetValue(category, out var entry))
                {
                    entry = (0, new List<string>());
                }

                if (entry.Examples.Count < ExamplesPerCategory && !entry.Examples.Contains(example))
                {
                    entry.Examples.Add(example);
                }

                _byCategory[category] = (entry.Count + 1, entry.Examples);
            }

            public string Report(int sequenceCount)
            {
                var report = new StringBuilder($"{Count} violations over {sequenceCount} sequences");

                foreach (var (category, (count, examples)) in _byCategory)
                {
                    report.AppendLine().AppendLine().Append($"{category} ({count}):");

                    foreach (var example in examples)
                    {
                        report.AppendLine().Append("  ").Append(example);
                    }
                }

                return report.ToString();
            }
        }

        private sealed class InspectableSystemFontCollection : SystemFontCollection
        {
            public InspectableSystemFontCollection(IFontManagerImpl platformImpl) : base(platformImpl)
            {
            }

            /// <summary>Every key the glyph typeface is cached under, in any family.</summary>
            public IReadOnlyCollection<FontCollectionKey> GetCachedKeys(GlyphTypeface glyphTypeface)
            {
                var keys = new HashSet<FontCollectionKey>();

                foreach (var family in _glyphTypefaceCache.Values)
                {
                    foreach (var entry in family)
                    {
                        if (ReferenceEquals(entry.Value, glyphTypeface))
                        {
                            keys.Add(new FontCollectionKey(entry.Key.Style, entry.Key.Weight, entry.Key.Stretch));
                        }
                    }
                }

                return keys;
            }
        }

        /// <summary>Counts every match a font collection asks of the platform font manager it wraps.</summary>
        private sealed class CountingFontManagerImpl : IFontManagerImpl
        {
            /// <summary>
            /// A typeface created from a font stream. A synthetic face is a variant of the platform
            /// typeface it is built from, so this is a copy of the whole font file.
            /// </summary>
            public const string StreamCall = "typeface from a stream";

            private readonly IFontManagerImpl _inner;
            private readonly ConcurrentDictionary<string, int> _calls = new();

            public CountingFontManagerImpl(IFontManagerImpl inner)
            {
                _inner = inner;
            }

            public void Reset() => _calls.Clear();

            public IEnumerable<string> Calls() => _calls.Keys.OrderBy(x => x, StringComparer.Ordinal);

            public IEnumerable<KeyValuePair<string, int>> Repeated() =>
                _calls.Where(x => x.Value > 1).OrderBy(x => x.Key, StringComparer.Ordinal);

            public string GetDefaultFontFamilyName() => _inner.GetDefaultFontFamilyName();

            public string[] GetInstalledFontFamilyNames(bool checkForUpdates = false) =>
                _inner.GetInstalledFontFamilyNames(checkForUpdates);

            public bool TryMatchCharacter(int codepoint, FontStyle fontStyle, FontWeight fontWeight,
                FontStretch fontStretch, string? familyName, CultureInfo? culture,
                [NotNullWhen(true)] out IPlatformTypeface? platformTypeface)
            {
                Count($"character match U+{codepoint:X4} via {familyName ?? "fallback"} " +
                      $"{fontStyle} {fontWeight} {fontStretch}");

                return _inner.TryMatchCharacter(codepoint, fontStyle, fontWeight, fontStretch, familyName, culture,
                    out platformTypeface);
            }

            public bool TryCreateGlyphTypeface(string familyName, FontStyle style, FontWeight weight,
                FontStretch stretch, [NotNullWhen(true)] out IPlatformTypeface? platformTypeface)
            {
                Count($"family match {familyName} {style} {weight} {stretch}");

                return _inner.TryCreateGlyphTypeface(familyName, style, weight, stretch, out platformTypeface);
            }

            public bool TryCreateGlyphTypeface(Stream stream, FontSimulations fontSimulations,
                [NotNullWhen(true)] out IPlatformTypeface? platformTypeface)
            {
                Count($"{StreamCall} [{fontSimulations}]");

                return _inner.TryCreateGlyphTypeface(stream, fontSimulations, out platformTypeface);
            }

            public bool TryGetFamilyTypefaces(string familyName,
                [NotNullWhen(true)] out IReadOnlyList<Typeface>? familyTypefaces)
            {
                Count($"family typefaces {familyName}");

                return _inner.TryGetFamilyTypefaces(familyName, out familyTypefaces);
            }

            private void Count(string call) => _calls.AddOrUpdate(call, 1, (_, count) => count + 1);
        }
    }
}
