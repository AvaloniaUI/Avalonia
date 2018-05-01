// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Avalonia.Media
{
    public class FontFamily
    {
        public FontFamily(string familyName) : this(familyName, null) { }

        public FontFamily(string familyName, Uri baseUri)
        {
            if (familyName == null) throw new ArgumentNullException(nameof(familyName));

            FontFamilyKey = new FontFamilyKey(familyName, baseUri);
        }

        public string Name => FontFamilyKey.FriendlyName;

        public Uri BaseUri => FontFamilyKey.BaseUri;

        internal FontFamilyKey FontFamilyKey { get; }
    }

    public class FamilyTypeface
    {
        public FamilyTypeface(Uri resourceUri = null, FontWeight fontWeight = FontWeight.Normal, FontStyle fontStyle = FontStyle.Normal)
        {
            ResourceUri = resourceUri;
            FontStyle = fontStyle;
            FontWeight = fontWeight;
        }

        public Uri ResourceUri { get; }
        public FontWeight FontWeight { get; }
        public FontStyle FontStyle { get; }
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

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)2166136261;

                if (FriendlyName != null)
                {
                    hash = (hash * 16777619) ^ FriendlyName.GetHashCode();
                }

                if (BaseUri != null)
                {
                    hash = (hash * 16777619) ^ BaseUri.GetHashCode();
                }

                return hash;
            }
        }

        public override bool Equals(object obj)
        {
            if (!(obj is FontFamilyKey other)) return false;

            if (FriendlyName != other.FriendlyName) return false;

            if (BaseUri != other.BaseUri) return false;

            return true;
        }
    }

    public class FamilyTypefaceKey
    {
        public FamilyTypefaceKey(FontWeight fontWeight = FontWeight.Normal, FontStyle fontStyle = FontStyle.Normal)
        {
            FontWeight = fontWeight;
            FontStyle = fontStyle;
        }

        public FontWeight FontWeight { get; }

        public FontStyle FontStyle { get; }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)2166136261;

                hash = (hash * 16777619) ^ FontWeight.GetHashCode();

                hash = (hash * 16777619) ^ FontStyle.GetHashCode();

                return hash;
            }
        }

        public override bool Equals(object obj)
        {
            if (!(obj is FamilyTypefaceKey other)) return false;

            if (FontWeight != other.FontWeight) return false;

            if (FontStyle != other.FontStyle) return false;

            return true;
        }
    }

    public class CachedFontFamily
    {
        private readonly ConcurrentDictionary<FamilyTypefaceKey, FamilyTypeface> _typefaces =
            new ConcurrentDictionary<FamilyTypefaceKey, FamilyTypeface>();

        public IEnumerable<FamilyTypeface> SupportedTypefaces => _typefaces.Values;

        public bool TryGetFamilyTypeface(out FamilyTypeface typeface, FontWeight fontWeight = FontWeight.Normal,
            FontStyle fontStyle = FontStyle.Normal)
        {
            return _typefaces.TryGetValue(new FamilyTypefaceKey(fontWeight, fontStyle), out typeface);
        }

        public FamilyTypeface GetOrAddFamilyTypeface(Uri resourceUri, FontWeight fontWeight = FontWeight.Normal, FontStyle fontStyle = FontStyle.Normal)
        {
            return _typefaces.GetOrAdd(new FamilyTypefaceKey(fontWeight, fontStyle), x => CreateFamilyTypeface(x, resourceUri));
        }

        private static FamilyTypeface CreateFamilyTypeface(FamilyTypefaceKey familyTypefaceKey, Uri resourceUri)
        {
            return new FamilyTypeface(resourceUri, familyTypefaceKey.FontWeight, familyTypefaceKey.FontStyle);
        }
    }

    public interface IFontFamilyCache
    {
        CachedFontFamily GetOrAddFontFamily(FontFamily fontFamily);
    }

    public class FontFamilyCache : IFontFamilyCache
    {
        private readonly ConcurrentDictionary<FontFamilyKey, CachedFontFamily> _cachedFontFamilies = new ConcurrentDictionary<FontFamilyKey, CachedFontFamily>();

        public CachedFontFamily GetOrAddFontFamily(FontFamily fontFamily)
        {
            return _cachedFontFamilies.GetOrAdd(fontFamily.FontFamilyKey, CreateCachedFontFamily);
        }

        private static CachedFontFamily CreateCachedFontFamily(FontFamilyKey fontFamilyKey)
        {
            return new CachedFontFamily();
        }
    }
}
