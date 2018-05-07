// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
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
            Name = name ?? throw new ArgumentNullException(nameof(name));
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
        public string Name { get; }

        /// <summary>
        /// Gets the key.
        /// </summary>
        /// <value>
        /// The key.
        /// </value>
        public FontFamilyKey Key { get; }

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

            if (fontFamilyExpression.Length == 1)
            {
                return new FontFamily(s);
            }

            string familyName;

            Uri source = null;

            switch (fontFamilyExpression.Length)
            {
                case 1:
                {
                    familyName = fontFamilyExpression[0];
                    break;
                }
                case 2:
                {
                    source = new Uri(fontFamilyExpression[0], UriKind.RelativeOrAbsolute);
                    familyName = fontFamilyExpression[1];
                    break;
                }
                default:
                {
                    throw new ArgumentException("Specified family is not supported.");
                }
            }

            return new FontFamily(familyName, source);
        }
    }
}
