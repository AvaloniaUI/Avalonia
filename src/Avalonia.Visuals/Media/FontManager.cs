using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Media.Fonts;
using Avalonia.Platform;

namespace Avalonia.Media
{
    /// <summary>
    ///     The font manager is used to query the system's installed fonts and is responsible for caching loaded fonts.
    ///     It is also responsible for the font fallback.
    /// </summary>
    public sealed class FontManager
    {
        private readonly ConcurrentDictionary<FontKey, Typeface> _typefaceCache =
            new ConcurrentDictionary<FontKey, Typeface>();
        private readonly FontFamily _defaultFontFamily;

        public FontManager(IFontManagerImpl platformImpl)
        {
            PlatformImpl = platformImpl;

            DefaultFontFamilyName = PlatformImpl.GetDefaultFontFamilyName();

            if (string.IsNullOrEmpty(DefaultFontFamilyName))
            {
                throw new InvalidOperationException("Default font family name can't be null or empty.");
            }

            _defaultFontFamily = new FontFamily(DefaultFontFamilyName);
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

                var fontManagerImpl = AvaloniaLocator.Current.GetService<IFontManagerImpl>();

                if (fontManagerImpl == null)
                    throw new InvalidOperationException("No font manager implementation was registered.");

                current = new FontManager(fontManagerImpl);

                AvaloniaLocator.CurrentMutable.Bind<FontManager>().ToConstant(current);

                return current;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public IFontManagerImpl PlatformImpl { get; }

        /// <summary>
        ///     Gets the system's default font family's name.
        /// </summary>
        public string DefaultFontFamilyName
        {
            get;
        }

        /// <summary>
        ///     Get all installed font family names.
        /// </summary>
        /// <param name="checkForUpdates">If <c>true</c> the font collection is updated.</param>
        public IEnumerable<string> GetInstalledFontFamilyNames(bool checkForUpdates = false) =>
            PlatformImpl.GetInstalledFontFamilyNames(checkForUpdates);

        /// <summary>
        ///     Returns a new typeface, or an existing one if a matching typeface exists.
        /// </summary>
        /// <param name="fontFamily">The font family.</param>
        /// <param name="fontStyle">The font style.</param>
        /// <param name="fontWeight">The font weight.</param>
        /// <returns>
        ///     The typeface.
        /// </returns>
        public Typeface GetOrAddTypeface(FontFamily fontFamily, FontStyle fontStyle = FontStyle.Normal,
            FontWeight fontWeight = FontWeight.Normal)
        {
            while (true)
            {
                if (fontFamily.IsDefault)
                {
                    fontFamily = _defaultFontFamily;
                }

                var key = new FontKey(fontFamily.Name, fontStyle, fontWeight);

                if (_typefaceCache.TryGetValue(key, out var typeface))
                {
                    return typeface;
                }

                typeface = new Typeface(fontFamily, fontStyle, fontWeight);

                if (_typefaceCache.TryAdd(key, typeface))
                {
                    return typeface;
                }

                if (fontFamily == _defaultFontFamily)
                {
                    return null;
                }

                fontFamily = _defaultFontFamily;
            }
        }

        /// <summary>
        ///     Tries to match a specified character to a typeface that supports specified font properties.
        ///     Returns <c>null</c> if no fallback was found.
        /// </summary>
        /// <param name="codepoint">The codepoint to match against.</param>
        /// <param name="fontStyle">The font style.</param>
        /// <param name="fontWeight">The font weight.</param>
        /// <param name="fontFamily">The font family. This is optional and used for fallback lookup.</param>
        /// <param name="culture">The culture.</param>
        /// <returns>
        ///     The matched typeface.
        /// </returns>
        public Typeface MatchCharacter(int codepoint,
            FontStyle fontStyle = FontStyle.Normal,
            FontWeight fontWeight = FontWeight.Normal,
            FontFamily fontFamily = null, CultureInfo culture = null)
        {
            foreach (var cachedTypeface in _typefaceCache.Values)
            {
                // First try to find a cached typeface by style and weight to avoid redundant glyph index lookup.
                if (cachedTypeface.Style == fontStyle && cachedTypeface.Weight == fontWeight
                                                      && cachedTypeface.GlyphTypeface.GetGlyph((uint)codepoint) != 0)
                {
                    return cachedTypeface;
                }
            }

            var matchedTypeface = PlatformImpl.TryMatchCharacter(codepoint, fontStyle, fontWeight, fontFamily, culture, out var key) ?
                _typefaceCache.GetOrAdd(key, new Typeface(key.FamilyName, key.Style, key.Weight)) :
                null;

            return matchedTypeface;
        }
    }
}
