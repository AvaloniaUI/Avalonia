// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media.Fonts;

namespace Avalonia.Media
{
    public class FontFamily
    {
        internal static FontFamily Default = new FontFamily("Courier New");

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Avalonia.Media.FontFamily" /> class.
        /// </summary>
        /// <param name="name">The name of the <see cref="FontFamily"/>.</param>
        /// <exception cref="T:System.ArgumentNullException">name</exception>
        public FontFamily(string name)
        {
            if (name == null) throw new ArgumentNullException();
            FamilyNames = new FamilyNameCollection(new[] { name });
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Avalonia.Media.FontFamily" /> class.
        /// </summary>
        /// <param name="names">The names of the <see cref="FontFamily"/>.</param>
        /// <exception cref="T:System.ArgumentNullException">name</exception>
        public FontFamily(IEnumerable<string> names)
        {
            if (names == null) throw new ArgumentNullException();
            FamilyNames = new FamilyNameCollection(names);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Avalonia.Media.FontFamily" /> class.
        /// </summary>
        /// <param name="name">The name of the <see cref="FontFamily"/>.</param>
        /// <param name="source">The source of font resources.</param>
        public FontFamily(string name, Uri source) : this(name)
        {
            Key = new FontFamilyKey(source);
        }

        /// <summary>
        /// Gets the name of the font family.
        /// </summary>
        /// <value>
        /// The name of the font family.
        /// </value>
        public string Name => FamilyNames.PrimaryFamilyName;

        /// <summary>
        /// Gets the family names.
        /// </summary>
        /// <value>
        /// The family familyNames.
        /// </value>
        internal FamilyNameCollection FamilyNames
        {
            get;
        }

        /// <summary>
        /// Gets the key for associated resources.
        /// </summary>
        /// <value>
        /// The family familyNames.
        /// </value>
        internal FontFamilyKey Key { get; }

        /// <summary>
        /// Returns a <see cref="string" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        /// Parses a <see cref="T:Avalonia.Media.FontFamily"/> string.
        /// </summary>
        /// <param name="s">The <see cref="T:Avalonia.Media.FontFamily"/> string.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">
        /// Specified family is not supported.
        /// </exception>
        public static FontFamily Parse(string s)
        {
            if (string.IsNullOrEmpty(s)) throw new ArgumentException("Specified family is not supported.");

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
                        return new FontFamily(segments[1], new Uri(segments[0], UriKind.RelativeOrAbsolute));
                    }
                default:
                    {
                        throw new ArgumentException("Specified family is not supported.");
                    }
            }
        }
    }
}
