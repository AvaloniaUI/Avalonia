// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

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
    public class FontManager
    {
        private readonly ConcurrentDictionary<FontKey, Typeface> _typefaceCache =
            new ConcurrentDictionary<FontKey, Typeface>();
        private readonly IFontManagerImpl _platformImpl;

        public FontManager(IFontManagerImpl platformImpl)
        {
            _platformImpl = platformImpl;

            DefaultFontFamilyName = _platformImpl.GetDefaultFontFamilyName();
        }

        public static FontManager Current => AvaloniaLocator.Current.GetService<FontManager>();

        /// <summary>
        ///     Gets the system's default font family's name.
        /// </summary>
        public string DefaultFontFamilyName
        {
            get;
        }

        /// <summary>
        ///     Get all installed fonts.
        /// <param name="checkForUpdates">If <c>true</c> the font collection is updated.</param>
        /// </summary>
        public IEnumerable<string> GetInstalledFontFamilyNames(bool checkForUpdates = false) =>
            _platformImpl.GetInstalledFontFamilyNames(checkForUpdates);

        /// <summary>
        ///     Returns a new typeface, or an existing one if a matching typeface exists.
        /// </summary>
        /// <param name="fontFamily">The font family.</param>
        /// <param name="fontWeight">The font weight.</param>
        /// <param name="fontStyle">The font style.</param>
        /// <returns>
        ///     The typeface.
        /// </returns>
        public Typeface GetOrAddTypeface(FontFamily fontFamily, FontWeight fontWeight = FontWeight.Normal,
            FontStyle fontStyle = FontStyle.Normal)
        {
            if (fontFamily.IsDefault)
            {
                fontFamily = new FontFamily(DefaultFontFamilyName);
            }

            var key = new FontKey(fontFamily, fontWeight, fontStyle);

            return _typefaceCache.GetOrAdd(key, new Typeface(fontFamily, fontWeight, fontStyle));
        }

        /// <summary>
        ///     Tries to match a specified character to a typeface that supports specified font properties.
        ///     Returns <c>null</c> if no fallback was found.
        /// </summary>
        /// <param name="codepoint">The codepoint to match against.</param>
        /// <param name="fontWeight">The font weight.</param>
        /// <param name="fontStyle">The font style.</param>
        /// <param name="fontFamily">The font family. This is optional and used for fallback lookup.</param>
        /// <param name="culture">The culture.</param>
        /// <returns>
        ///     The matched typeface.
        /// </returns>
        public Typeface MatchCharacter(int codepoint, FontWeight fontWeight = FontWeight.Normal,
            FontStyle fontStyle = FontStyle.Normal,
            FontFamily fontFamily = null, CultureInfo culture = null)
        {
            var key = _platformImpl.MatchCharacter(codepoint, fontWeight, fontStyle, fontFamily, culture);

            return _typefaceCache.GetOrAdd(key, new Typeface(key.FontFamily, key.Weight, key.Style));
        }
    }
}
