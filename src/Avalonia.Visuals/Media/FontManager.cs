// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;
using System.Globalization;
using Avalonia.Platform;

namespace Avalonia.Media
{
    public static class FontManager
    {
        private static readonly IFontManagerImpl s_platformImpl = GetPlatformImpl();

        /// <inheritdoc cref="IFontManagerImpl.DefaultFontFamilyName"/>
        public static string DefaultFontFamilyName => s_platformImpl.DefaultFontFamilyName;

        /// <inheritdoc cref="IFontManagerImpl.GetInstalledFontFamilyNames"/>
        public static IEnumerable<string> GetInstalledFontFamilyNames(bool checkForUpdates = false) =>
            s_platformImpl.GetInstalledFontFamilyNames(checkForUpdates);

        /// <inheritdoc cref="IFontManagerImpl.GetTypeface"/>
        public static Typeface GetTypeface(FontFamily fontFamily, FontWeight fontWeight, FontStyle fontStyle) =>
            s_platformImpl.GetTypeface(fontFamily, fontWeight, fontStyle);

        /// <inheritdoc cref="IFontManagerImpl.MatchCharacter"/>
        public static Typeface MatchCharacter(int codepoint, FontWeight fontWeight = default,
            FontStyle fontStyle = default,
            FontFamily fontFamily = null, CultureInfo culture = null) =>
            s_platformImpl.MatchCharacter(codepoint, fontWeight, fontStyle, fontFamily, culture);

        private static IFontManagerImpl GetPlatformImpl()
        {
            var platformImpl = AvaloniaLocator.Current.GetService<IFontManagerImpl>();

            return platformImpl ?? new EmptyFontManagerImpl();
        }

        private class EmptyFontManagerImpl : IFontManagerImpl
        {
            public string DefaultFontFamilyName => "Arial";

            public IEnumerable<string> GetInstalledFontFamilyNames(bool checkForUpdates = false) => new[] { "Arial" };

            public Typeface GetTypeface(FontFamily fontFamily, FontWeight fontWeight, FontStyle fontStyle)
            {
                return new Typeface(fontFamily, fontWeight, fontStyle);
            }

            public Typeface MatchCharacter(int codepoint, FontWeight fontWeight = default, FontStyle fontStyle = default,
                FontFamily fontFamily = null, CultureInfo culture = null)
            {
                return null;
            }
        }
    }
}
