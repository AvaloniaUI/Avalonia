// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;
using System.Globalization;
using Avalonia.Platform;

namespace Avalonia.Media
{
    /// <summary>
    ///     The font manager is used to query the system's installed fonts and is responsible for caching loaded fonts.
    ///     It is also responsible for the font fallback.
    /// </summary>
    public abstract class FontManager
    {
        public static readonly FontManager Default = CreateDefault();

        /// <summary>
        ///     Gets the system's default font family's name.
        /// </summary>
        public string DefaultFontFamilyName
        {
            get;
            protected set;
        }

        /// <summary>
        ///     Get all installed fonts in the system.
        /// <param name="checkForUpdates">If <c>true</c> the font collection is updated.</param>
        /// </summary>
        public abstract IEnumerable<string> GetInstalledFontFamilyNames(bool checkForUpdates = false);

        /// <summary>
        ///     Get a cached typeface from specified parameters.
        /// </summary>
        /// <param name="fontFamily">The font family.</param>
        /// <param name="fontWeight">The font weight.</param>
        /// <param name="fontStyle">The font style.</param>
        /// <returns>
        ///     The cached typeface.
        /// </returns>
        public abstract Typeface GetCachedTypeface(FontFamily fontFamily, FontWeight fontWeight, FontStyle fontStyle);

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
        public abstract Typeface MatchCharacter(int codepoint, FontWeight fontWeight = default,
            FontStyle fontStyle = default,
            FontFamily fontFamily = null, CultureInfo culture = null);

        public static FontManager CreateDefault()
        {
            var platformImpl = AvaloniaLocator.Current.GetService<IFontManagerImpl>();

            if (platformImpl != null)
            {
                return new PlatformFontManager(platformImpl);
            }

            return new EmptyFontManager();
        }

        private class PlatformFontManager : FontManager
        {
            private readonly IFontManagerImpl _platformImpl;

            public PlatformFontManager(IFontManagerImpl platformImpl)
            {
                _platformImpl = platformImpl;

                DefaultFontFamilyName = _platformImpl.DefaultFontFamilyName;
            }

            public override IEnumerable<string> GetInstalledFontFamilyNames(bool checkForUpdates = false) =>
                _platformImpl.GetInstalledFontFamilyNames(checkForUpdates);

            public override Typeface GetCachedTypeface(FontFamily fontFamily, FontWeight fontWeight, FontStyle fontStyle) =>
                _platformImpl.GetTypeface(fontFamily, fontWeight, fontStyle);

            public override Typeface MatchCharacter(int codepoint, FontWeight fontWeight = default,
                FontStyle fontStyle = default,
                FontFamily fontFamily = null, CultureInfo culture = null) =>
                _platformImpl.MatchCharacter(codepoint, fontWeight, fontStyle, fontFamily, culture);
        }

        private class EmptyFontManager : FontManager
        {
            public EmptyFontManager()
            {
                DefaultFontFamilyName = "Empty";
            }

            public override IEnumerable<string> GetInstalledFontFamilyNames(bool checkForUpdates = false) =>
                new[] { DefaultFontFamilyName };

            public override Typeface GetCachedTypeface(FontFamily fontFamily, FontWeight fontWeight, FontStyle fontStyle) => new Typeface(fontFamily, fontWeight, fontStyle);

            public override Typeface MatchCharacter(int codepoint, FontWeight fontWeight = default,
                FontStyle fontStyle = default,
                FontFamily fontFamily = null, CultureInfo culture = null) => null;
        }
    }
}
