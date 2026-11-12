#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using Avalonia.Media;
using Avalonia.Platform;
using SkiaSharp;

namespace Avalonia.Skia.UnitTests.Media
{
    /// <summary>
    /// Font manager that answers family and character matches the way DirectWrite answers them,
    /// backed by embedded test fonts.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A request is answered with the nearest real face of the family, which is often not the
    /// requested one: SemiBold gets a real Bold, and ExtraBold on a family whose heaviest face is Bold
    /// gets that Bold. A bold request on a face of Medium or lighter, and a slanted request on an
    /// upright face, are simulated inside the returned typeface, which then reports Bold or Oblique,
    /// and the family lists those simulated faces next to its real ones.
    /// </para>
    /// <para>
    /// A colour font face matched to a bold or slanted request is matched again as the regular
    /// upright face, as the Skia backend does, since a simulation applied inside the typeface cannot
    /// be removed from it afterwards.
    /// </para>
    /// <para>
    /// The returned typeface is wrapped the way the Skia backend wraps a platform match: a simulated
    /// typeface reports the requested values, and the simulations applied inside it are kept apart
    /// from the ones applied when it is drawn. Every typeface handed out maps back to the face of
    /// the model it was created from.
    /// </para>
    /// </remarks>
    internal sealed class DirectWriteLikeFontManagerImpl : IFontManagerImpl, IDisposable
    {
        public const string InterFamily = "Inter";
        public const string NotoMonoFamily = "Noto Mono";
        public const string SourceSerifItalicFamily = "Source Serif 4 36pt";
        public const string EmojiFamily = "Twitter Color Emoji";
        public const string CjkFamily = "Noto Sans JP";

        private const string RenderAssets = "resm:Avalonia.Skia.UnitTests.Assets.";
        private const string SkiaFonts = "resm:Avalonia.Skia.UnitTests.Fonts.";
        private const string Assembly = "?assembly=Avalonia.Skia.UnitTests";

        private readonly Dictionary<string, SKData> _fontData = new();
        private readonly Dictionary<SKTypeface, Face> _faceByTypeface = new(ReferenceEqualityComparer.Instance);

        public DirectWriteLikeFontManagerImpl()
        {
            Families = new[]
            {
                // A regular and a bold face only, the shape of Tahoma.
                new Family(InterFamily, new[]
                {
                    StaticFace("Inter-Regular", RenderAssets + "Inter-Regular.ttf", FontStyle.Normal, FontWeight.Normal),
                    StaticFace("Inter-Bold", RenderAssets + "Inter-Bold.ttf", FontStyle.Normal, FontWeight.Bold)
                }),

                // A single regular face, simulated for every bold or slanted request.
                new Family(NotoMonoFamily, new[]
                {
                    StaticFace("NotoMono-Regular", RenderAssets + "NotoMono-Regular.ttf", FontStyle.Normal,
                        FontWeight.Normal)
                }),

                // A single italic face, which also answers an upright request.
                new Family(SourceSerifItalicFamily, new[]
                {
                    StaticFace("SourceSerif4-Italic", RenderAssets + "SourceSerif4_36pt-Italic.ttf", FontStyle.Italic,
                        FontWeight.Normal)
                }),

                // A single regular colour face.
                new Family(EmojiFamily, new[]
                {
                    new Face("TwitterColorEmoji", RenderAssets + "TwitterColorEmoji-SVGinOT.ttf", FontStyle.Normal,
                        FontWeight.Normal, IsColor: true)
                }),

                // A single Thin face, the only one covering the CJK test codepoint.
                new Family(CjkFamily, new[]
                {
                    StaticFace("NotoSansJP-Subset", SkiaFonts + "NotoSansJP-Subset.ttf", FontStyle.Normal,
                        FontWeight.Thin)
                })
            };
        }

        /// <summary>The installed families, in the order character fallback tries them.</summary>
        public IReadOnlyList<Family> Families { get; }

        /// <summary>Number of colour font faces matched again as their regular upright face.</summary>
        public int RegularColourFaceMatches { get; private set; }

        /// <summary>Gets the face of the model a typeface handed out by this font manager was created from.</summary>
        public Face GetFace(SKTypeface skTypeface) => _faceByTypeface.TryGetValue(skTypeface, out var face) ?
            face :
            throw new InvalidOperationException($"{skTypeface.FamilyName} was not created by this font manager.");

        public Family GetFamily(string familyName) => Families.Single(x =>
            string.Equals(x.Name, familyName, StringComparison.OrdinalIgnoreCase));

        public string GetDefaultFontFamilyName() => InterFamily;

        public string[] GetInstalledFontFamilyNames(bool checkForUpdates = false) =>
            Families.Select(x => x.Name).ToArray();

        public bool TryCreateGlyphTypeface(string familyName, FontStyle style, FontWeight weight,
            FontStretch stretch, [NotNullWhen(true)] out IPlatformTypeface? platformTypeface)
        {
            var family = FindFamily(familyName);

            if (family is null)
            {
                platformTypeface = null;

                return false;
            }

            platformTypeface = CreatePlatformTypeface(family, style, weight, stretch);

            return true;
        }

        public bool TryMatchCharacter(int codepoint, FontStyle fontStyle, FontWeight fontWeight,
            FontStretch fontStretch, string? familyName, CultureInfo? culture,
            [NotNullWhen(true)] out IPlatformTypeface? platformTypeface)
        {
            // The requested family first when it covers the codepoint, then the fallback order.
            var candidates = Families.OrderBy(x => IsNamed(x, familyName) ? 0 : 1);

            foreach (var family in candidates)
            {
                if (Covers(family, codepoint))
                {
                    platformTypeface = CreatePlatformTypeface(family, fontStyle, fontWeight, fontStretch);

                    return true;
                }
            }

            platformTypeface = null;

            return false;
        }

        public bool TryCreateGlyphTypeface(Stream stream, FontSimulations fontSimulations,
            [NotNullWhen(true)] out IPlatformTypeface? platformTypeface)
        {
            var skTypeface = SKTypeface.FromStream(stream);

            if (skTypeface is null)
            {
                platformTypeface = null;

                return false;
            }

            platformTypeface = new SkiaTypeface(skTypeface, fontSimulations);

            return true;
        }

        public bool TryGetFamilyTypefaces(string familyName,
            [NotNullWhen(true)] out IReadOnlyList<Typeface>? familyTypefaces)
        {
            var family = FindFamily(familyName);

            if (family is null)
            {
                familyTypefaces = null;

                return false;
            }

            var typefaces = new List<Typeface>();
            var hasBoldFace = family.Faces.Any(x => x.Weight >= FontWeight.SemiBold);

            foreach (var face in family.Faces)
            {
                typefaces.Add(new Typeface(family.Name, face.Style, face.Weight, face.Stretch));

                // The faces DirectWrite simulates are listed next to the real ones.
                if (face.Style == FontStyle.Normal)
                {
                    typefaces.Add(new Typeface(family.Name, FontStyle.Oblique, face.Weight, face.Stretch));
                }

                if (!hasBoldFace && face.Weight <= FontWeight.Medium)
                {
                    typefaces.Add(new Typeface(family.Name, face.Style, FontWeight.Bold, face.Stretch));
                }
            }

            familyTypefaces = typefaces;

            return true;
        }

        public void Dispose()
        {
            foreach (var data in _fontData.Values)
            {
                data.Dispose();
            }

            _fontData.Clear();
            _faceByTypeface.Clear();
        }

        private Family? FindFamily(string familyName) => Families.FirstOrDefault(x => IsNamed(x, familyName));

        private static bool IsNamed(Family family, string? familyName) =>
            familyName is not null && string.Equals(family.Name, familyName, StringComparison.OrdinalIgnoreCase);

        private IPlatformTypeface CreatePlatformTypeface(Family family, FontStyle style, FontWeight weight,
            FontStretch stretch)
        {
            var answer = Match(family, style, weight, stretch);

            if (answer.Face.IsColor && answer.PlatformSimulations != FontSimulations.None)
            {
                RegularColourFaceMatches++;

                answer = Match(family, FontStyle.Normal, FontWeight.Normal, stretch);
            }

            return Wrap(Load(answer.Face), answer, style, weight, stretch);
        }

        /// <summary>The face DirectWrite picks for a request, and what it simulates inside it.</summary>
        private static PlatformAnswer Match(Family family, FontStyle style, FontWeight weight, FontStretch stretch)
        {
            // Stretch first, then style, then weight, the order of the CSS font matching algorithm.
            var byStretch = family.Faces
                .GroupBy(x => Math.Abs((int)x.Stretch - (int)stretch))
                .OrderBy(x => x.Key)
                .First();

            var byStyle = byStretch
                .GroupBy(x => StyleRank(style, x.Style))
                .OrderBy(x => x.Key)
                .First()
                .ToArray();

            var face = NearestWeight(byStyle, weight);

            var platformSimulations = FontSimulations.None;
            var reportedWeight = face.Weight;
            var reportedStyle = face.Style;

            if (weight >= FontWeight.SemiBold && face.Weight <= FontWeight.Medium)
            {
                platformSimulations |= FontSimulations.Bold;
                reportedWeight = FontWeight.Bold;
            }

            if (style != FontStyle.Normal && face.Style == FontStyle.Normal)
            {
                platformSimulations |= FontSimulations.Oblique;
                reportedStyle = FontStyle.Oblique;
            }

            return new PlatformAnswer(face, platformSimulations, reportedStyle, reportedWeight, face.Stretch);
        }

        private static int StyleRank(FontStyle requested, FontStyle candidate)
        {
            if (requested == candidate)
            {
                return 0;
            }

            // Italic and oblique stand in for each other before an upright face does, and an upright
            // request takes an oblique face before an italic one.
            return requested switch
            {
                FontStyle.Normal => candidate == FontStyle.Oblique ? 1 : 2,
                _ => candidate == FontStyle.Normal ? 2 : 1
            };
        }

        /// <summary>The weight matching of CSS Fonts Level 4, which DirectWrite follows.</summary>
        private static Face NearestWeight(IReadOnlyList<Face> faces, FontWeight weight)
        {
            var desired = (int)weight;

            IEnumerable<Face> order;

            if (desired is >= 400 and <= 500)
            {
                order = faces.Where(x => (int)x.Weight >= desired && (int)x.Weight <= 500).OrderBy(x => x.Weight)
                    .Concat(faces.Where(x => (int)x.Weight < desired).OrderByDescending(x => x.Weight))
                    .Concat(faces.Where(x => (int)x.Weight > 500).OrderBy(x => x.Weight));
            }
            else if (desired < 400)
            {
                order = faces.Where(x => (int)x.Weight <= desired).OrderByDescending(x => x.Weight)
                    .Concat(faces.Where(x => (int)x.Weight > desired).OrderBy(x => x.Weight));
            }
            else
            {
                order = faces.Where(x => (int)x.Weight >= desired).OrderBy(x => x.Weight)
                    .Concat(faces.Where(x => (int)x.Weight < desired).OrderByDescending(x => x.Weight));
            }

            return order.First();
        }

        /// <summary>Wraps a platform answer the way the Skia backend wraps a typeface its platform matched.</summary>
        private static SkiaTypeface Wrap(SKTypeface skTypeface, PlatformAnswer answer, FontStyle style,
            FontWeight weight, FontStretch stretch)
        {
            var fontSimulations = FontSimulations.None;

            if (weight >= FontWeight.SemiBold && answer.Weight < weight &&
                (answer.PlatformSimulations & FontSimulations.Bold) == 0)
            {
                fontSimulations |= FontSimulations.Bold;
            }

            if (style != FontStyle.Normal && answer.Style == FontStyle.Normal)
            {
                fontSimulations |= FontSimulations.Oblique;
            }

            var allSimulations = fontSimulations | answer.PlatformSimulations;

            var reportedWeight = (allSimulations & FontSimulations.Bold) != 0 ? weight : answer.Weight;
            var reportedStyle = (allSimulations & FontSimulations.Oblique) != 0 ? style : answer.Style;

            return new SkiaTypeface(skTypeface, fontSimulations, answer.PlatformSimulations, reportedStyle,
                reportedWeight, answer.Stretch);
        }

        private bool Covers(Family family, int codepoint)
        {
            using var skTypeface = SKTypeface.FromData(GetData(family.Faces[0].Uri));

            return skTypeface?.ContainsGlyph(codepoint) == true;
        }

        private SKTypeface Load(Face face)
        {
            var skTypeface = SKTypeface.FromData(GetData(face.Uri)) ??
                             throw new InvalidOperationException($"Could not load the test font {face.Uri}.");

            _faceByTypeface[skTypeface] = face;

            return skTypeface;
        }

        private SKData GetData(string uri)
        {
            if (!_fontData.TryGetValue(uri, out var data))
            {
                var assetLoader = AvaloniaLocator.Current.GetRequiredService<IAssetLoader>();

                using var stream = assetLoader.Open(new Uri(uri + Assembly, UriKind.Absolute));

                _fontData[uri] = data = SKData.Create(stream);
            }

            return data;
        }

        private static Face StaticFace(string name, string uri, FontStyle style, FontWeight weight) =>
            new(name, uri, style, weight, IsColor: false);

        /// <summary>A real face of the model: a static face or a colour face.</summary>
        public sealed record Face(string Name, string Uri, FontStyle Style, FontWeight Weight, bool IsColor)
        {
            public FontStretch Stretch => FontStretch.Normal;
        }

        public sealed record Family(string Name, IReadOnlyList<Face> Faces)
        {
            public bool HasUprightFace => Faces.Any(x => x.Style == FontStyle.Normal);
        }

        /// <param name="Face">The real face picked.</param>
        /// <param name="PlatformSimulations">The simulations applied inside the returned typeface.</param>
        /// <param name="Style">The style the returned typeface reports.</param>
        /// <param name="Weight">The weight the returned typeface reports.</param>
        /// <param name="Stretch">The stretch the returned typeface reports.</param>
        private readonly record struct PlatformAnswer(Face Face, FontSimulations PlatformSimulations,
            FontStyle Style, FontWeight Weight, FontStretch Stretch);
    }
}
