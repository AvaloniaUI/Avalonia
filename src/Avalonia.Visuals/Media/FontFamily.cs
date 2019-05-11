// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media.Fonts;
using Avalonia.Platform;

namespace Avalonia.Media
{
    public class FontFamily
    {
        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Avalonia.Media.FontFamily" /> class.
        /// </summary>
        /// <param name="name">The name of the <see cref="T:Avalonia.Media.FontFamily" />.</param>
        public FontFamily(string name) : this(null, name)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Avalonia.Media.FontFamily" /> class.
        /// </summary>
        /// <param name="baseUri">Specifies the base uri that is used to resolve font family assets.</param>
        /// <param name="name">The name of the <see cref="T:Avalonia.Media.FontFamily" />.</param>
        /// <exception cref="T:System.ArgumentException">Base uri must be an absolute uri.</exception>
        public FontFamily(Uri baseUri, string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                FamilyNames = new FamilyNameCollection(string.Empty);

                return;
            }

            var fontFamilySegment = GetFontFamilyIdentifier(name);

            if (fontFamilySegment.Source != null)
            {
                if (baseUri != null && !baseUri.IsAbsoluteUri)
                {
                    throw new ArgumentException("Base uri must be an absolute uri.", nameof(baseUri));
                }

                Key = new FontFamilyKey(fontFamilySegment.Source, baseUri);
            }

            FamilyNames = new FamilyNameCollection(fontFamilySegment.Name);
        }

        /// <summary>
        /// Represents the default font family
        /// </summary>
        public static FontFamily Default => new FontFamily(string.Empty);

        /// <summary>
        /// Represents all font families in the system. This can be an expensive call depending on platform implementation.
        /// </summary>
        public static IEnumerable<FontFamily> SystemFontFamilies =>
            AvaloniaLocator.Current.GetService<IPlatformRenderInterface>().InstalledFontNames.Select(name => new FontFamily(name));

        /// <summary>
        /// Gets the primary family name of the font family.
        /// </summary>
        /// <value>
        /// The primary name of the font family.
        /// </value>
        public string Name => FamilyNames.PrimaryFamilyName;

        /// <summary>
        /// Gets the family names.
        /// </summary>
        /// <value>
        /// The family familyNames.
        /// </value>
        public FamilyNameCollection FamilyNames { get; }

        /// <summary>
        /// Gets the key for associated assets.
        /// </summary>
        /// <value>
        /// The family familyNames.
        /// </value>
        public FontFamilyKey Key { get; }

        /// <summary>
        /// Implicit conversion of string to FontFamily
        /// </summary>
        /// <param name="s"></param>
        public static implicit operator FontFamily(string s)
        {
            return new FontFamily(s);
        }

        private struct FontFamilyIdentifier
        {
            public FontFamilyIdentifier(string name, Uri source)
            {
                Name = name;
                Source = source;
            }

            public string Name { get; }

            public Uri Source { get; }
        }

        private static FontFamilyIdentifier GetFontFamilyIdentifier(string name)
        {
            var segments = name.Split('#');

            switch (segments.Length)
            {
                case 1:
                    {
                        return new FontFamilyIdentifier(segments[0], null);
                    }

                case 2:
                    {
                        var source = segments[0].StartsWith("/")
                            ? new Uri(segments[0], UriKind.Relative)
                            : new Uri(segments[0], UriKind.RelativeOrAbsolute);

                        return new FontFamilyIdentifier(segments[1], source);
                    }

                default:
                    {
                        throw new ArgumentException("Specified family is not supported.");
                    }
            }
        }

        /// <summary>
        /// Parses a <see cref="T:Avalonia.Media.FontFamily"/> string.
        /// </summary>
        /// <param name="s">The <see cref="T:Avalonia.Media.FontFamily"/> string.</param>
        /// <param name="baseUri">Specifies the base uri that is used to resolve font family assets.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">
        /// Specified family is not supported.
        /// </exception>
        public static FontFamily Parse(string s, Uri baseUri = null)
        {
            if (string.IsNullOrEmpty(s))
            {
                throw new ArgumentException("Specified family is not supported.", nameof(s));
            }

            return new FontFamily(baseUri, s);
        }

        /// <summary>
        /// Returns a <see cref="string" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            if (Key != null)
            {
                return Key + "#" + FamilyNames;
            }

            return FamilyNames.ToString();
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)2186146271;

                hash = (hash * 15768619) ^ FamilyNames.GetHashCode();

                if (Key != null)
                {
                    hash = (hash * 15768619) ^ Key.GetHashCode();
                }

                return hash;
            }
        }

        public override bool Equals(object obj)
        {
            if (!(obj is FontFamily other))
            {
                return false;
            }

            if (Key != null)
            {
                return other.FamilyNames.Equals(FamilyNames) && other.Key.Equals(Key);
            }

            return other.FamilyNames.Equals(FamilyNames);
        }
    }
}
