// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Platform;

namespace Avalonia.Media
{
    public abstract class FontManager : IFontManagerImpl
    {
        public static readonly FontManager Default = CreateDefaultFontManger();

        /// <inheritdoc cref="IFontManagerImpl"/>
        public string DefaultFontFamilyName { get; protected set; }

        private static FontManager CreateDefaultFontManger()
        {
            var platformImpl = AvaloniaLocator.Current.GetService<IFontManagerImpl>();

            if(platformImpl == null)
            {
                return new EmptyFontManager();
            }

            return new PlatformFontManger(platformImpl);
        }

        /// <inheritdoc cref="IFontManagerImpl"/>
        public abstract IEnumerable<string> GetInstalledFontFamilyNames(bool checkForUpdates = false);

        /// <inheritdoc cref="IFontManagerImpl"/>
        public abstract IGlyphTypefaceImpl CreateGlyphTypeface(Typeface typeface);

        /// <inheritdoc cref="IFontManagerImpl"/>
        public abstract Typeface GetTypeface(FontFamily fontFamily, FontWeight fontWeight, FontStyle fontStyle);

        /// <inheritdoc cref="IFontManagerImpl"/>
        public abstract Typeface MatchCharacter(int codepoint, FontWeight fontWeight = default,
            FontStyle fontStyle = default,
            FontFamily fontFamily = null, CultureInfo culture = null);

        private class PlatformFontManger : FontManager
        {
            private readonly IFontManagerImpl _platformImpl;

            public PlatformFontManger(IFontManagerImpl platformImpl)
            {
                _platformImpl = platformImpl;

                DefaultFontFamilyName = _platformImpl.DefaultFontFamilyName;
            }

            public override IEnumerable<string> GetInstalledFontFamilyNames(bool checkForUpdates = false) =>
                _platformImpl.GetInstalledFontFamilyNames(checkForUpdates);

            public override IGlyphTypefaceImpl CreateGlyphTypeface(Typeface typeface) => _platformImpl.CreateGlyphTypeface(typeface);

            public override Typeface GetTypeface(FontFamily fontFamily, FontWeight fontWeight, FontStyle fontStyle) =>
                _platformImpl.GetTypeface(fontFamily, fontWeight, fontStyle);

            public override Typeface MatchCharacter(int codepoint, FontWeight fontWeight = default,
                FontStyle fontStyle = default,
                FontFamily fontFamily = null, CultureInfo culture = null) =>
                _platformImpl.MatchCharacter(codepoint, fontWeight, fontStyle, fontFamily, culture);
        }

        private class EmptyFontManager : FontManager
        {
            private readonly string[] _defaultFontFamilies = { "Arial" };

            public EmptyFontManager()
            {
                DefaultFontFamilyName = "Arial";
            }

            public override IEnumerable<string> GetInstalledFontFamilyNames(bool checkForUpdates = false)
            {
                return _defaultFontFamilies;
            }

            public override IGlyphTypefaceImpl CreateGlyphTypeface(Typeface typeface)
            {
                throw new NotSupportedException();
            }

            public override Typeface GetTypeface(FontFamily fontFamily, FontWeight fontWeight, FontStyle fontStyle)
            {
                throw new NotSupportedException();
            }

            public override Typeface MatchCharacter(int codepoint, FontWeight fontWeight = default, FontStyle fontStyle = default,
                FontFamily fontFamily = null, CultureInfo culture = null)
            {
                throw new NotSupportedException();
            }
        }
    }
}
