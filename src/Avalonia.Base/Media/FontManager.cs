using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Avalonia.Logging;
using Avalonia.Media.Fonts;
using Avalonia.Platform;
using Avalonia.Utilities;

namespace Avalonia.Media
{
    /// <summary>
    ///     The font manager is used to query the system's installed fonts and is responsible for caching loaded fonts.
    ///     It is also responsible for the font fallback.
    /// </summary>
    public sealed class FontManager : IDisposable
    {
        internal static Uri SystemFontsKey = new Uri("fonts:SystemFonts", UriKind.Absolute);

        public const string FontCollectionScheme = "fonts";
        public const string SystemFontScheme = "systemfont";
        public const string CompositeFontScheme = "compositefont";

        private readonly ConcurrentDictionary<Uri, IFontCollection> _fontCollections = new ConcurrentDictionary<Uri, IFontCollection>();
        private readonly IReadOnlyList<FontFallback>? _fontFallbacks;
        private readonly IReadOnlyDictionary<string, FontFamily>? _fontFamilyMappings;

        public FontManager(IFontManagerImpl platformImpl)
        {
            PlatformImpl = platformImpl;

            var options = AvaloniaLocator.Current.GetService<FontManagerOptions>();
            _fontFallbacks = options?.FontFallbacks;
            _fontFamilyMappings = options?.FontFamilyMappings;

            var defaultFontFamilyName = GetDefaultFontFamilyName(options);
            DefaultFontFamily = new FontFamily(defaultFontFamilyName);
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
        public IFontCollection SystemFonts
        {
            get
            {
                if (TryGetFontCollection(SystemFontsKey, out var fontCollection))
                {
                    return fontCollection;
                }

                // Fallback to an empty system font collection
                return new EmptySystemFontCollection();
            }
        }

        internal IFontManagerImpl PlatformImpl { get; }

        /// <summary>
        ///     Tries to get a glyph typeface for specified typeface.
        /// </summary>
        /// <param name="typeface">The typeface.</param>
        /// <param name="glyphTypeface">The created glyphTypeface</param>
        /// <returns>
        ///     <c>True</c>, if the <see cref="FontManager"/> could create the glyph typeface, <c>False</c> otherwise.
        /// </returns>
        public bool TryGetGlyphTypeface(Typeface typeface, [NotNullWhen(true)] out GlyphTypeface? glyphTypeface)
        {
            glyphTypeface = null;

            var fontFamily = GetMappedFontFamily(typeface.FontFamily);

            if (typeface.FontFamily.Name == FontFamily.DefaultFontFamilyName)
            {
                return TryGetGlyphTypeface(new Typeface(DefaultFontFamily, typeface.Style, typeface.Weight, typeface.Stretch), out glyphTypeface);
            }


            if (fontFamily.Key != null)
            {
                if (fontFamily.Key is CompositeFontFamilyKey compositeKey)
                {
                    for (var i = 0; i < compositeKey.Keys.Count; i++)
                    {
                        var key = compositeKey.Keys[i];

                        var familyName = fontFamily.FamilyNames[i];

                        if (_fontFamilyMappings != null && _fontFamilyMappings.TryGetValue(familyName, out var mappedFontFamily))
                        {
                            if (mappedFontFamily.Key != null)
                            {
                                key = mappedFontFamily.Key;
                            }
                            else
                            {
                                key = new FontFamilyKey(SystemFontsKey);
                            }

                            familyName = mappedFontFamily.FamilyNames.PrimaryFamilyName;
                        }

                        if (familyName == FontFamily.DefaultFontFamilyName)
                        {
                            return TryGetGlyphTypeface(new Typeface(DefaultFontFamily, typeface.Style, typeface.Weight, typeface.Stretch), out glyphTypeface);
                        }

                        if (TryGetGlyphTypefaceByKeyAndName(typeface, key, familyName, out glyphTypeface) &&
                            glyphTypeface.FamilyName.Contains(familyName))
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    var familyName = fontFamily.FamilyNames.PrimaryFamilyName;

                    if (TryGetGlyphTypefaceByKeyAndName(typeface, fontFamily.Key, familyName, out glyphTypeface))
                    {
                        return true;
                    }

                    return false;
                }
            }
            else
            {
                var familyName = fontFamily.FamilyNames.PrimaryFamilyName;

                if (SystemFonts.TryGetGlyphTypeface(familyName, typeface.Style, typeface.Weight, typeface.Stretch, out glyphTypeface))
                {
                    return true;
                }
            }

            if (typeface.FontFamily == DefaultFontFamily)
            {
                return false;
            }

            //Nothing was found so use the default
            return TryGetGlyphTypeface(new Typeface(DefaultFontFamily, typeface.Style, typeface.Weight, typeface.Stretch), out glyphTypeface);

            FontFamily GetMappedFontFamily(FontFamily fontFamily)
            {
                if (_fontFamilyMappings == null || !_fontFamilyMappings.TryGetValue(fontFamily.FamilyNames.PrimaryFamilyName, out var mappedFontFamily))
                {
                    return fontFamily;
                }

                return mappedFontFamily;
            }
        }

        private bool TryGetGlyphTypefaceByKeyAndName(Typeface typeface, FontFamilyKey key, string familyName, [NotNullWhen(true)] out GlyphTypeface? glyphTypeface)
        {
            var source = key.Source.EnsureAbsolute(key.BaseUri);

            if (TryGetFontCollection(source, out var fontCollection))
            {
                if (fontCollection.TryGetGlyphTypeface(familyName, typeface.Style, typeface.Weight, typeface.Stretch,
                        out glyphTypeface))
                {
                    return true;
                }

                var logger = Logger.TryGet(LogEventLevel.Debug, "FontManager");

                logger?.Log(this,
                    $"Font family '{familyName}' could not be found. Present font families: [{string.Join(",", fontCollection)}]");

                return false;
            }

            glyphTypeface = null;

            return false;
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

                        if (TryGetGlyphTypeface(typeface, out var glyphTypeface) && glyphTypeface.CharacterToGlyphMap.TryGetGlyph(codepoint, out _))
                        {
                            return true;
                        }
                    }
                }
            }

            //Try to match against fallbacks first
            if (fontFamily?.Key != null)
            {
                if (fontFamily.Key is CompositeFontFamilyKey compositeKey)
                {
                    for (int i = 0; i < compositeKey.Keys.Count; i++)
                    {
                        var key = compositeKey.Keys[i];
                        var familyName = fontFamily.FamilyNames[i];
                        var source = key.Source.EnsureAbsolute(key.BaseUri);

                        if (familyName == FontFamily.DefaultFontFamilyName)
                        {
                            familyName = DefaultFontFamily.Name;
                        }

                        if (TryGetFontCollection(source, out var fontCollection) &&
                            // With composite fonts we need to first check if the font collection contains the family if not we skip it
                            fontCollection.TryGetGlyphTypeface(familyName, fontStyle, fontWeight, fontStretch, out _) &&
                            fontCollection.TryMatchCharacter(codepoint, fontStyle, fontWeight, fontStretch, familyName, culture, out typeface))
                        {
                            if (typeface.FontFamily.Name == DefaultFontFamily.Name && i + 1 < compositeKey.Keys.Count)
                            {
                                continue;
                            }

                            return true;
                        }
                    }
                }

                var fontUri = fontFamily.Key.Source.EnsureAbsolute(fontFamily.Key.BaseUri);

                if (fontUri.IsFontCollection())
                {
                    if (TryGetFontCollection(fontUri, out var fontCollection) &&
                            fontCollection.TryMatchCharacter(codepoint, fontStyle, fontWeight, fontStretch, fontFamily.Name, culture, out typeface))
                    {
                        return true;
                    }
                }
            }

            //Try to find a match with the system font collection
            return SystemFonts.TryMatchCharacter(codepoint, fontStyle, fontWeight, fontStretch, fontFamily?.Name,
                culture, out typeface);
        }

        internal IReadOnlyList<Typeface> GetFamilyTypefaces(FontFamily fontFamily)
        {
            var key = fontFamily.Key;

            if (key == null)
            {
                if (SystemFonts.TryGetFamilyTypefaces(fontFamily.Name, out var familyTypefaces))
                {
                    return familyTypefaces;
                }
            }
            else
            {
                var source = key.Source.EnsureAbsolute(key.BaseUri);

                if (TryGetFontCollection(source, out var fontCollection) && fontCollection.TryGetFamilyTypefaces(fontFamily.Name, out var familyTypefaces))
                {
                    return familyTypefaces;
                }
            }

            return [];
        }

        private bool TryGetFontCollection(Uri source, [NotNullWhen(true)] out IFontCollection? fontCollection)
        {
            Debug.Assert(source.IsAbsoluteUri);

            if (source.Scheme == SystemFontScheme)
            {
                source = SystemFontsKey;
            }

            if (!_fontCollections.TryGetValue(source, out fontCollection))
            {
                if (source == SystemFontsKey)
                {
                    fontCollection = new SystemFontCollection(PlatformImpl);
                }
                else
                {
                    if (source.IsAbsoluteResm() || source.IsAvares())
                    {
                        fontCollection = new EmbeddedFontCollection(source, source);
                    }
                }

                if (fontCollection != null)
                {
                    return _fontCollections.TryAdd(fontCollection.Key, fontCollection);
                }
            }

            return fontCollection != null;
        }

        private string GetDefaultFontFamilyName(FontManagerOptions? options)
        {
            var defaultFontFamilyName = options?.DefaultFamilyName
                ?? PlatformImpl.GetDefaultFontFamilyName();

            if (string.IsNullOrEmpty(defaultFontFamilyName) && SystemFonts.Count > 0)
            {
                defaultFontFamilyName = SystemFonts[0].Name;
            }

            if (string.IsNullOrEmpty(defaultFontFamilyName))
            {
                throw new InvalidOperationException(
                    "Default font family name can't be null or empty.");
            }

            if (defaultFontFamilyName == FontFamily.DefaultFontFamilyName)
            {
                throw new InvalidOperationException(
                    $"'{FontFamily.DefaultFontFamilyName}' is a placeholder and cannot be used as the default font family name. Provide a concrete font family name via {nameof(FontManagerOptions)} or the platform implementation.");
            }

            return defaultFontFamilyName;
        }

        void IDisposable.Dispose()
        {
            foreach (var pair in _fontCollections)
                pair.Value.Dispose();

            _fontCollections.Clear();
            (PlatformImpl as IDisposable)?.Dispose();
        }
    }
}
