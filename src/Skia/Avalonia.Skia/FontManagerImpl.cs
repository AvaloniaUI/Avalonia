#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using Avalonia.Media;
using Avalonia.Media.Fonts.Tables;
using Avalonia.Platform;
using SkiaSharp;

namespace Avalonia.Skia
{
    internal class FontManagerImpl : IFontManagerImpl
    {
        private SKFontManager _skFontManager = SKFontManager.Default;

        public string GetDefaultFontFamilyName()
        {

            return SKTypeface.Default.FamilyName;
        }

        public string[] GetInstalledFontFamilyNames(bool checkForUpdates = false)
        {
            if (checkForUpdates)
            {
                _skFontManager = SKFontManager.CreateDefault();
            }

            return _skFontManager.GetFontFamilies();
        }

        [ThreadStatic] private static string[]? t_languageTagBuffer;

        public bool TryMatchCharacter(
            int codepoint,
            FontStyle fontStyle,
            FontWeight fontWeight,
            FontStretch fontStretch,
            string? familyName,
            CultureInfo? culture,
            [NotNullWhen(returnValue: true)] out IPlatformTypeface? platformTypeface)
        {
            if (!TryMatchCharacter(codepoint, fontStyle, fontWeight, fontStretch, familyName, culture, out SKTypeface? skTypeface))
            {
                platformTypeface = null;

                return false;
            }

            platformTypeface = CreatePlatformTypeface(skTypeface, fontStyle, fontWeight, fontStretch);

            return true;

        }

        private bool TryMatchCharacter(
            int codepoint,
            FontStyle fontStyle,
            FontWeight fontWeight,
            FontStretch fontStretch,
            string? familyName,
            CultureInfo? culture,
            [NotNullWhen(true)] out SKTypeface? skTypeface)
        {
            SKFontStyle skFontStyle;

            switch (fontWeight)
            {
                case FontWeight.Normal when fontStyle == FontStyle.Normal && fontStretch == FontStretch.Normal:
                    skFontStyle = SKFontStyle.Normal;
                    break;
                case FontWeight.Normal when fontStyle == FontStyle.Italic && fontStretch == FontStretch.Normal:
                    skFontStyle = SKFontStyle.Italic;
                    break;
                case FontWeight.Bold when fontStyle == FontStyle.Normal && fontStretch == FontStretch.Normal:
                    skFontStyle = SKFontStyle.Bold;
                    break;
                case FontWeight.Bold when fontStyle == FontStyle.Italic && fontStretch == FontStretch.Normal:
                    skFontStyle = SKFontStyle.BoldItalic;
                    break;
                default:
                    skFontStyle = new SKFontStyle((SKFontStyleWeight)fontWeight, (SKFontStyleWidth)fontStretch, fontStyle.ToSkia());
                    break;
            }

            culture ??= CultureInfo.CurrentUICulture;

            t_languageTagBuffer ??= new string[1];
            t_languageTagBuffer[0] = culture.Name;

            skTypeface = _skFontManager.MatchCharacter(string.IsNullOrEmpty(familyName) ? null : familyName, skFontStyle, t_languageTagBuffer, codepoint);

            return skTypeface != null;
        }

        public bool TryCreateGlyphTypeface(string familyName, FontStyle style, FontWeight weight,
            FontStretch stretch, [NotNullWhen(true)] out IPlatformTypeface? platformTypeface)
        {
            platformTypeface = null;

            var fontStyle = new SKFontStyle((SKFontStyleWeight)weight, (SKFontStyleWidth)stretch, style.ToSkia());

            var skTypeface = _skFontManager.MatchFamily(familyName, fontStyle);

            if (skTypeface is null)
            {
                return false;
            }

            platformTypeface = CreatePlatformTypeface(skTypeface, style, weight, stretch);

            return true;
        }

        /// <summary>
        /// Wraps a typeface the platform matched to the requested style, weight and stretch. A bold
        /// or slanted request the matched face cannot satisfy is simulated on that face when it is
        /// drawn, without a copy of the font data, unless the face is a colour font face.
        /// </summary>
        internal static SkiaTypeface CreatePlatformTypeface(SKTypeface skTypeface, FontStyle style, FontWeight weight,
            FontStretch stretch, Func<string, SKFontStyle, SKTypeface?>? matchFamily = null)
        {
            return CreatePlatformTypeface(skTypeface, skTypeface.FontStyle.Slant.ToAvalonia(),
                (FontWeight)skTypeface.FontWeight, (FontStretch)skTypeface.FontWidth, GetPlatformSimulations(skTypeface),
                style, weight, stretch, SkiaTypeface.IsColorFont(skTypeface));
        }

        /// <param name="skTypeface">The typeface the platform matched.</param>
        /// <param name="faceStyle">The style the matched typeface reports.</param>
        /// <param name="faceWeight">The weight the matched typeface reports.</param>
        /// <param name="faceStretch">The stretch the matched typeface reports.</param>
        /// <param name="platformSimulations">The simulations the platform applied inside the matched typeface.</param>
        /// <param name="style">The requested style.</param>
        /// <param name="weight">The requested weight.</param>
        /// <param name="stretch">The requested stretch.</param>
        /// <param name="isColorFont">Whether the matched typeface draws its glyphs from a colour table.</param>
        internal static SkiaTypeface CreatePlatformTypeface(SKTypeface skTypeface, FontStyle faceStyle,
            FontWeight faceWeight, FontStretch faceStretch, FontSimulations platformSimulations, FontStyle style,
            FontWeight weight, FontStretch stretch, bool isColorFont)
        {
            var fontSimulations = FontSimulations.None;

            // A face the platform already emboldened is not emboldened a second time, while a face
            // designed lighter than the request is, a real Bold face for ExtraBold included.
            if (weight >= FontWeight.SemiBold && faceWeight < weight && (platformSimulations & FontSimulations.Bold) == 0)
            {
                fontSimulations |= FontSimulations.Bold;
            }

            // Any slanted face, italic or oblique, satisfies a slanted request.
            if (style != FontStyle.Normal && faceStyle == FontStyle.Normal)
            {
                fontSimulations |= FontSimulations.Oblique;
            }

            // A colour font face answers a bold or slanted request as designed, so it reports its
            // own weight and style. Simulations the platform applied itself are kept as they are.
            if (fontSimulations != FontSimulations.None && SkiaTypeface.IsColorFont(skTypeface))
            {
                fontSimulations = FontSimulations.None;
            }

            var allSimulations = fontSimulations | platformSimulations;

            // A simulated face stands for the requested one, so it reports the requested values and
            // a font collection recognizes it as the face it asked for.
            var reportedWeight = (allSimulations & FontSimulations.Bold) != 0 ? weight : faceWeight;
            var reportedStyle = (allSimulations & FontSimulations.Oblique) != 0 ||
                                style != FontStyle.Normal && faceStyle != FontStyle.Normal ?
                style :
                faceStyle;
            var reportedStretch = allSimulations != FontSimulations.None ? stretch : faceStretch;

            return new SkiaTypeface(skTypeface, fontSimulations, platformSimulations, reportedStyle, reportedWeight,
                reportedStretch);
        }

        /// <summary>
        /// Gets the simulations a platform applied inside a typeface it matched, such as a DirectWrite
        /// face that reports Bold or an oblique slant for a font file designed regular and upright.
        /// </summary>
        private static FontSimulations GetPlatformSimulations(SKTypeface skTypeface)
        {
            if (!skTypeface.TryGetTableData(OS2Table.Tag, out var os2) || os2 is null ||
                !OS2Table.TryLoad(os2, out var table))
            {
                return FontSimulations.None;
            }

            return GetPlatformSimulations(skTypeface.FontWeight, skTypeface.FontStyle.Slant, table.WeightClass,
                table.Selection, isVariable: false, hasSlantAxis: false);
        }

        /// <summary>
        /// Gets the simulations a platform applied inside a typeface from what the typeface reports
        /// and what its font file declares.
        /// </summary>
        /// <param name="reportedWeight">The weight the typeface reports.</param>
        /// <param name="reportedSlant">The slant the typeface reports.</param>
        /// <param name="weightClass">The usWeightClass of the OS/2 table.</param>
        /// <param name="fsSelection">The fsSelection flags of the OS/2 table.</param>
        /// <param name="isVariable">Whether the font has an fvar table.</param>
        /// <param name="hasSlantAxis">Whether the fvar table declares a slnt or ital axis.</param>
        internal static FontSimulations GetPlatformSimulations(int reportedWeight, SKFontStyleSlant reportedSlant,
            int weightClass, OS2Table.FontSelectionFlags fsSelection, bool isVariable, bool hasSlantAxis)
        {
            // Some fonts store the weight class on the legacy 1-9 scale.
            if (weightClass is > 0 and < 10)
            {
                weightClass *= 100;
            }

            var platformSimulations = FontSimulations.None;

            if (reportedWeight >= (int)FontWeight.SemiBold && weightClass < (int)FontWeight.SemiBold &&
                (fsSelection & OS2Table.FontSelectionFlags.BOLD) == 0)
            {
                platformSimulations |= FontSimulations.Bold;
            }

            if (reportedSlant != SKFontStyleSlant.Upright &&
                (fsSelection & (OS2Table.FontSelectionFlags.ITALIC | OS2Table.FontSelectionFlags.OBLIQUE)) == 0)
            {
                platformSimulations |= FontSimulations.Oblique;
            }

            return platformSimulations;
        }

        /// <summary>
        /// Replaces a colour font face matched to a bold or slanted request with the regular upright
        /// face of its family.
        /// </summary>
        /// <param name="skTypeface">The typeface the platform matched. Disposed when it is replaced.</param>
        /// <param name="isColorFont">Whether <paramref name="skTypeface"/> draws its glyphs from a colour table.</param>
        /// <param name="style">The requested style.</param>
        /// <param name="weight">The requested weight.</param>
        /// <param name="stretch">The requested stretch.</param>
        /// <param name="matchFamily">Matches a family name and style on the platform.</param>
        internal static SKTypeface MatchRegularColourFace(SKTypeface skTypeface, bool isColorFont, FontStyle style,
            FontWeight weight, FontStretch stretch, Func<string, SKFontStyle, SKTypeface?> matchFamily)
        {
            return skTypeface;
        }

        public bool TryCreateGlyphTypeface(Stream stream, FontSimulations fontSimulations, [NotNullWhen(true)] out IPlatformTypeface? platformTypeface)
        {
            var skTypeface = SKTypeface.FromStream(stream);

            if (skTypeface != null)
            {
                if (fontSimulations != FontSimulations.None && SkiaTypeface.IsColorFont(skTypeface))
                {
                    fontSimulations = FontSimulations.None;
                }

                platformTypeface = new SkiaTypeface(skTypeface, fontSimulations);

                return true;
            }

            platformTypeface = null;

            return false;
        }

        public bool TryGetFamilyTypefaces(string familyName, [NotNullWhen(true)] out IReadOnlyList<Typeface>? familyTypefaces)
        {
            familyTypefaces = null;

            var set = _skFontManager.GetFontStyles(familyName);

            if (set.Count == 0)
            {
                return false;
            }

            var typefaces = new List<Typeface>(set.Count);

            foreach (var fontStyle in set)
            {
                typefaces.Add(new Typeface(familyName, fontStyle.Slant.ToAvalonia(), (FontWeight)fontStyle.Weight, (FontStretch)fontStyle.Width));
            }

            familyTypefaces = typefaces;

            return true;
        }
    }
}
