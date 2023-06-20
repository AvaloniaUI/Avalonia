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

            var defaultFontFamilyName = options?.DefaultFamilyName ?? PlatformImpl.GetDefaultFontFamilyName();

            if (string.IsNullOrEmpty(defaultFontFamilyName))
            {
                throw new InvalidOperationException("Default font family name can't be null or empty.");
            }

            DefaultFontFamily = new FontFamily(defaultFontFamilyName);

            AddFontCollection(new SystemFontCollection(this));
        }

        /// <summary>
        /// Get the current font manager instance.
        /// </summary>
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
        ///     Gets the system's default font family.
        /// </summary>
        public FontFamily DefaultFontFamily
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

            if(typeface.FontFamily.Name == FontFamily.DefaultFontFamilyName)
            {
                return TryGetGlyphTypeface(new Typeface(DefaultFontFamily, typeface.Style, typeface.Weight, typeface.Stretch), out glyphTypeface);
            }

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

                if (!_fontCollections.TryGetValue(source, out var fontCollection) && (source.IsAbsoluteResm() || source.IsAvares()))
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

            for (var i = 0; i < fontFamily.FamilyNames.Count; i++)
            {
                var familyName = fontFamily.FamilyNames[i];

                if (SystemFonts.TryGetGlyphTypeface(familyName, typeface.Style, typeface.Weight, typeface.Stretch, out glyphTypeface))
                {
                    if (!fontFamily.FamilyNames.HasFallbacks || glyphTypeface.FamilyName != DefaultFontFamily.Name)
                    {
                        return true;
                    }
                }
            }

            if(typeface.FontFamily == DefaultFontFamily)
            {
                return false;
            }

            //Nothing was found so use the default
            return TryGetGlyphTypeface(new Typeface(FontFamily.DefaultFontFamilyName, typeface.Style, typeface.Weight, typeface.Stretch), out glyphTypeface);
        }

        /// <summary>
        /// Add a font collection to the manager.
        /// </summary>
        /// <param name="fontCollection">The font collection.</param>
        /// <exception cref="ArgumentException"></exception>
        /// <remarks>If a font collection's key is already present the collection is replaced.</remarks>
        public void AddFontCollection(IFontCollection fontCollection)
        {
            var key = fontCollection.Key;

            if (!fontCollection.Key.IsFontCollection())
            {
                throw new ArgumentException("Font collection Key should follow the fonts: scheme.", nameof(fontCollection));
            }

            _fontCollections.AddOrUpdate(key, fontCollection, (_, oldCollection) =>
            {
                oldCollection.Dispose();

                return fontCollection;
            });

            fontCollection.Initialize(PlatformImpl);
        }

        /// <summary>
        /// Removes the font collection that corresponds to specified key.
        /// </summary>
        /// <param name="key">The font collection's key.</param>
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
                    if (fallback.UnicodeRange.IsInRange(codepoint))
                    {
                        typeface = new Typeface(fallback.FontFamily, fontStyle, fontWeight, fontStretch);

                        if (TryGetGlyphTypeface(typeface, out var glyphTypeface) && glyphTypeface.TryGetGlyph((uint)codepoint, out _))
                        {
                            return true;
                        }
                    }
                }
            }

            //Try to match against fallbacks first
            if (fontFamily != null && fontFamily.FamilyNames.HasFallbacks)
            {
                for (int i = 1; i < fontFamily.FamilyNames.Count; i++)
                {
                    var familyName = fontFamily.FamilyNames[i];

                    foreach (var fontCollection in _fontCollections.Values)
                    {
                        if (fontCollection.TryMatchCharacter(codepoint, fontStyle, fontWeight, fontStretch, familyName, culture, out typeface))
                        {
                            return true;
                        };
                    }
                }
            }

            //Try to find a match with the system font manager
            return PlatformImpl.TryMatchCharacter(codepoint, fontStyle, fontWeight, fontStretch, culture, out typeface);
        }
    }
}
