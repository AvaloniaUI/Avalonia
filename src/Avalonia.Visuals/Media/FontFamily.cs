// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using Avalonia.Media.Fonts;

namespace Avalonia.Media
{
    public class FontFamily
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FontFamily"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <exception cref="ArgumentNullException">name</exception>
        public FontFamily(string name = "Courier New")
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            FamilyNames = new FamilyNameList(name);
        }

        public FontFamily(IEnumerable<string> names)
        {
            if (names == null) throw new ArgumentNullException(nameof(names));
            FamilyNames = new FamilyNameList(names);
        }

        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Avalonia.Media.FontFamily" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="source">The source.</param>
        public FontFamily(string name, Uri source) : this(name)
        {
            Key = new FontFamilyKey(source);
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name => FamilyNames.FirstFamilyName;

        /// <summary>
        /// Gets the key.
        /// </summary>
        /// <value>
        /// The key.
        /// </value>
        internal FontFamilyKey Key { get; }

        internal FamilyNameList FamilyNames { get; }

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
                return Key + "#" + Name;
            }

            return Name;
        }

        internal class FamilyNameList : List<string>
        {
            public FamilyNameList(string familyName)
            {
                Add(familyName);
                FirstFamilyName = familyName;
            }

            public FamilyNameList(IEnumerable<string> familyNames) : base(familyNames)
            {
                FirstFamilyName = this[0];
            }

            public string FirstFamilyName { get; }
        }

        /// <summary>
        /// Parses a <see cref="FontFamily"/> string.
        /// </summary>
        /// <param name="s">The <see cref="FontFamily"/> string.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">
        /// Specified family is not supported.
        /// </exception>
        public static FontFamily Parse(string s)
        {
            if (string.IsNullOrEmpty(s)) throw new ArgumentException("Specified family is not supported.");

            var fontFamilyExpression = s.Split('#');

            switch (fontFamilyExpression.Length)
            {
                case 1:
                    {
                        var familyNames = fontFamilyExpression[0].Split(';');
                        return new FontFamily(familyNames);
                    }
                case 2:
                    {
                        var source = new Uri(fontFamilyExpression[0], UriKind.RelativeOrAbsolute);
                        return new FontFamily(fontFamilyExpression[1], source);
                    }
                default:
                    {
                        throw new ArgumentException("Specified family is not supported.");
                    }
            }
        }
    }
}
