// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Avalonia.Media
{
    public class FontFamily
    {
        private IFontFamily _loadedFamily;

        public FontFamily(string familyName) : this(familyName, null) { }

        public FontFamily(string familyName, Uri baseUri)
        {
            if (familyName == null) throw new ArgumentNullException(nameof(familyName));

            FontFamilyKey = new FontFamilyKey(familyName, baseUri);
        }

        public string Name => FontFamilyKey.FriendlyName;

        public Uri BaseUri => FontFamilyKey.BaseUri;

        internal FontFamilyKey FontFamilyKey { get; }

        internal IFontFamily LoadedFamily => _loadedFamily ?? (_loadedFamily = AvaloniaLocator.Current
                                                 .GetService<IFontFamilyLoader>()
                                                 .LoadFontFamily(FontFamilyKey));

        public IEnumerable<FamilyTypeface> AvailableTypefaces => LoadedFamily.SupportedTypefaces;
    }

    public class FamilyTypeface
    {
        public FamilyTypeface()
        {
            FontStyle = FontStyle.Normal;
            FontWeight = FontWeight.Normal;
        }

        public FamilyTypeface(Typeface typeface)
        {
            FontStyle = typeface.Style;
            FontWeight = typeface.Weight;
        }

        public FontStyle FontStyle { get; }
        public FontWeight FontWeight { get; }
    }

    public class FontFamilyKey
    {
        public FontFamilyKey(string friendlyName) : this(friendlyName, null) { }

        public FontFamilyKey(string friendlyName, Uri baseUri)
        {
            FriendlyName = friendlyName;
            BaseUri = baseUri;
        }

        public string FriendlyName { get; }

        public Uri BaseUri { get; }
    }

    internal interface IFontFamily
    {
        IEnumerable<FamilyTypeface> SupportedTypefaces { get; }
    }

    internal class SystemFont : IFontFamily
    {
        public SystemFont() : this(new List<FamilyTypeface> { new FamilyTypeface() }) { }

        public SystemFont(IEnumerable<FamilyTypeface> supportedTypefaces)
        {
            SupportedTypefaces = new ReadOnlyCollection<FamilyTypeface>(new List<FamilyTypeface>(supportedTypefaces));
        }

        public IEnumerable<FamilyTypeface> SupportedTypefaces { get; }
    }

    internal class CustomFont : IFontFamily
    {
        public CustomFont() : this(new List<FamilyTypeface> { new FamilyTypeface() }) { }

        public CustomFont(IEnumerable<FamilyTypeface> supportedTypefaces)
        {
            SupportedTypefaces = new ReadOnlyCollection<FamilyTypeface>(new List<FamilyTypeface>(supportedTypefaces));
        }

        public IEnumerable<FamilyTypeface> SupportedTypefaces { get; }
    }

    internal interface IFontFamilyLoader
    {
        IFontFamily LoadFontFamily(FontFamilyKey fontFamilyKey);
    }
}
