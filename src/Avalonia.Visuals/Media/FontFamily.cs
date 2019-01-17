﻿// Copyright (c) The Avalonia Project. All rights reserved.
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
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Avalonia.Media.FontFamily" /> class.
        /// </summary>
        /// <param name="name">The name of the <see cref="FontFamily"/>.</param>
        /// <exception cref="T:System.ArgumentNullException">name</exception>
        public FontFamily(string name)
        {
            Contract.Requires<ArgumentNullException>(name != null);

            FamilyNames = new FamilyNameCollection(new[] { name });
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Avalonia.Media.FontFamily" /> class.
        /// </summary>
        /// <param name="names">The names of the <see cref="FontFamily"/>.</param>
        /// <exception cref="T:System.ArgumentNullException">name</exception>
        public FontFamily(IEnumerable<string> names)
        {
            Contract.Requires<ArgumentNullException>(names != null);

            FamilyNames = new FamilyNameCollection(names);
        }

        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Avalonia.Media.FontFamily" /> class.
        /// </summary>
        /// <param name="name">The name of the <see cref="T:Avalonia.Media.FontFamily" />.</param>
        /// <param name="source">The source of font resources.</param>
        /// <param name="baseUri"></param>
        public FontFamily(string name, Uri source, Uri baseUri = null) : this(name)
        {
            Key = new FontFamilyKey(source, baseUri);
        }

        /// <summary>
        /// Represents the default font family
        /// </summary>
        public static FontFamily Default => new FontFamily(String.Empty);

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
            return Parse(s);
        }

        /// <summary>
        /// Parses a <see cref="T:Avalonia.Media.FontFamily"/> string.
        /// </summary>
        /// <param name="s">The <see cref="T:Avalonia.Media.FontFamily"/> string.</param>
        /// <param name="baseUri"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">
        /// Specified family is not supported.
        /// </exception>
        public static FontFamily Parse(string s, Uri baseUri = null)
        {
            if (string.IsNullOrEmpty(s))
            {
                throw new ArgumentException("Specified family is not supported.");
            }

            var segments = s.Split('#');

            switch (segments.Length)
            {
                case 1:
                    {
                        var names = segments[0].Split(',')
                            .Select(x => x.Trim())
                            .Where(x => !string.IsNullOrWhiteSpace(x));
                        return new FontFamily(names);
                    }

                case 2:
                    {
                        var uri = segments[0].StartsWith("/")
                                      ? new Uri(segments[0], UriKind.Relative)
                                      : new Uri(segments[0], UriKind.RelativeOrAbsolute);

                        return uri.IsAbsoluteUri
                                   ? new FontFamily(segments[1], uri)
                                   : new FontFamily(segments[1], uri, baseUri);
                    }

                default:
                    {
                        throw new ArgumentException("Specified family is not supported.");
                    }
            }
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
