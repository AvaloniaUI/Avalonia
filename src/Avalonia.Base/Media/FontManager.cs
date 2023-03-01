using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Avalonia.Media.Fonts;
using Avalonia.Platform;
using Avalonia.Utilities;

namespace Avalonia.Media
{
    /// <summary>
    ///     The font manager is used to query the system's installed fonts and is responsible for caching loaded fonts.
    ///     It is also responsible for the font fallback.
    /// </summary>
    public sealed class FontManager
    {
        internal static Uri SystemFontsKey = new Uri("fonts:SystemFonts");

        public const string FontCollectionScheme = "fonts";

        private readonly ConcurrentDictionary<Uri, IFontCollection> _fontCollections = new ConcurrentDictionary<Uri, IFontCollection>();
        private readonly IReadOnlyList<FontFallback>? _fontFallbacks;

        public FontManager(IFontManagerImpl platformImpl)
        {
            PlatformImpl = platformImpl;

            var options = AvaloniaLocator.Current.GetService<FontManagerOptions>();

            _fontFallbacks = options?.FontFallbacks;

            DefaultFontFamilyName = options?.DefaultFamilyName ?? PlatformImpl.GetDefaultFontFamilyName();

            if (string.IsNullOrEmpty(DefaultFontFamilyName))
            {
                throw new InvalidOperationException("Default font family name can't be null or empty.");
            }

            AddFontCollection(new SystemFontCollection(this));
        }

        public static FontManager Current
        {
            get
            {
                var current = AvaloniaLocator.Current.GetService<FontManager>();

                if (current != null)
                {
                    return current;
                }

                var fontManagerImpl = AvaloniaLocator.Current.GetRequiredService<IFontManagerImpl>();

                current = new FontManager(fontManagerImpl);

                AvaloniaLocator.CurrentMutable.Bind<FontManager>().ToConstant(current);

                return current;
            }
        }

        /// <summary>
        ///     Gets the system's default font family's name.
        /// </summary>
        public string DefaultFontFamilyName
        {
            get;
        }

        /// <summary>
        ///     Get all system fonts.
        /// </summary>
        public IFontCollection SystemFonts => _fontCollections[SystemFontsKey];

        internal IFontManagerImpl PlatformImpl { get; }

        /// <summary>
        ///     Tries to get a glyph typeface for specified typeface.
        /// </summary>
        /// <param name="typeface">The typeface.</param>
        /// <param name="glyphTypeface">The created glyphTypeface</param>
        /// <returns>
        ///     <c>True</c>, if the <see cref="FontManager"/> could create the glyph typeface, <c>False</c> otherwise.
        /// </returns>
        public bool TryGetGlyphTypeface(Typeface typeface, [NotNullWhen(true)] out IGlyphTypeface? glyphTypeface)
        {
            glyphTypeface = null;

            var fontFamily = typeface.FontFamily;

            if (fontFamily.Key is FontFamilyKey key)
            {
                var source = key.Source;

                if (!source.IsAbsoluteUri)
                {
                    if (key.BaseUri == null)
                    {
                        throw new NotSupportedException($"{nameof(key.BaseUri)} can't be null.");
                    }

                    source = new Uri(key.BaseUri, source);
                }

                if (!_fontCollections.TryGetValue(source, out var fontCollection))
                {
                    var embeddedFonts = new EmbeddedFontCollection(source, source);

                    embeddedFonts.Initialize(PlatformImpl);

                    if (embeddedFonts.Count > 0 && _fontCollections.TryAdd(source, embeddedFonts))
                    {
                        fontCollection = embeddedFonts;
                    }
                }

                if (fontCollection != null && fontCollection.TryGetGlyphTypeface(fontFamily.FamilyNames.PrimaryFamilyName,
                    typeface.Style, typeface.Weight, typeface.Stretch, out glyphTypeface))
                {
                    return true;
                }

                if (!fontFamily.FamilyNames.HasFallbacks)
                {
                    return false;
                }
            }

            foreach (var familyName in fontFamily.FamilyNames)
            {
                if (SystemFonts.TryGetGlyphTypeface(familyName, typeface.Style, typeface.Weight, typeface.Stretch, out glyphTypeface))
                {
                    return true;
                }
            }

            return SystemFonts.TryGetGlyphTypeface(DefaultFontFamilyName, typeface.Style, typeface.Weight, typeface.Stretch, out glyphTypeface);
        }

        public void AddFontCollection(IFontCollection fontCollection)
        {
            var key = fontCollection.Key;

            if (!fontCollection.Key.IsFontCollection())
            {
                throw new ArgumentException(nameof(fontCollection), "Font collection Key should follow the fonts: scheme.");
            }

            _fontCollections.AddOrUpdate(key, fontCollection, (_, oldCollection) =>
            {
                oldCollection.Dispose();

                return fontCollection;
            });

            fontCollection.Initialize(PlatformImpl);
        }

        public void RemoveFontCollection(Uri key)
        {
            if (_fontCollections.TryRemove(key, out var fontCollection))
            {
                fontCollection.Dispose();
            }
        }

        /// <summary>
        ///     Tries to match a specified character to a <see cref="Typeface"/> that supports specified font properties.
        /// </summary>
        /// <param name="codepoint">The codepoint to match against.</param>
        /// <param name="fontStyle">The font style.</param>
        /// <param name="fontWeight">The font weight.</param>
        /// <param name="fontStretch">The font stretch.</param>
        /// <param name="fontFamily">The font family. This is optional and used for fallback lookup.</param>
        /// <param name="culture">The culture.</param>
        /// <param name="typeface">The matching <see cref="Typeface"/>.</param>
        /// <returns>
        ///     <c>True</c>, if the <see cref="FontManager"/> could match the character to specified parameters, <c>False</c> otherwise.
        /// </returns>
        public bool TryMatchCharacter(int codepoint, FontStyle fontStyle, FontWeight fontWeight,
            FontStretch fontStretch, FontFamily? fontFamily, CultureInfo? culture, out Typeface typeface)
        {
            if (_fontFallbacks != null)
            {
                foreach (var fallback in _fontFallbacks)
                {
                    typeface = new Typeface(fallback.FontFamily, fontStyle, fontWeight, fontStretch);

                    if (TryGetGlyphTypeface(typeface, out var glyphTypeface) && glyphTypeface.TryGetGlyph((uint)codepoint, out _))
                    {
                        return true;
                    }
                }
            }

            return PlatformImpl.TryMatchCharacter(codepoint, fontStyle, fontWeight, fontStretch, fontFamily, culture, out typeface);
        }
    }
}
