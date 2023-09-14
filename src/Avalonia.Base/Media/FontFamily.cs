using System;
using System.Collections.Generic;
using Avalonia.Media.Fonts;
using Avalonia.Utilities;

namespace Avalonia.Media
{
    public sealed class FontFamily
    {
        public const string DefaultFontFamilyName = "$Default";

        static FontFamily()
        {
            Default = new FontFamily(DefaultFontFamilyName);
        }

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
        public FontFamily(Uri? baseUri, string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            var fontSources = GetFontSourceIdentifier(name);

            FamilyNames = new FamilyNameCollection(fontSources);

            if (fontSources.Count == 1)
            {
                if(fontSources[0].Source is Uri source)
                {
                    if (baseUri != null && !baseUri.IsAbsoluteUri)
                    {
                        throw new ArgumentException("Base uri must be an absolute uri.", nameof(baseUri));
                    }

                    Key = new FontFamilyKey(source, baseUri);
                }
            }
            else
            {
                var keys = new FontFamilyKey[fontSources.Count];

                for (int i = 0; i < fontSources.Count; i++)
                {
                    var fontSource = fontSources[i];

                    if(fontSource.Source is not null)
                    {
                        keys[i] = new FontFamilyKey(fontSource.Source, baseUri);
                    }
                    else
                    {
                        keys[i] = new FontFamilyKey(new Uri(FontManager.SystemFontScheme + ":" + fontSource.Name, UriKind.Absolute));
                    }
                }

                Key = new CompositeFontFamilyKey(new Uri(FontManager.CompositeFontScheme + ":" + name, UriKind.Absolute), keys);
            }
        }

        /// <summary>
        /// Represents the default font family
        /// </summary>
        public static FontFamily Default { get; }

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
        /// The family key.
        /// </value>
        /// <remarks>Key is only used for custom fonts.</remarks>
        public FontFamilyKey? Key { get; }

        /// <summary>
        /// Implicit conversion of string to FontFamily
        /// </summary>
        /// <param name="s"></param>
        public static implicit operator FontFamily(string s)
        {
            return new FontFamily(s);
        }

        private static FrugalStructList<FontSourceIdentifier> GetFontSourceIdentifier(string name)
        {
            var result = new FrugalStructList<FontSourceIdentifier>(1);

            var segments = name.Split(',');

            for (int i = 0; i < segments.Length; i++)
            {
                var segment = segments[i];
                var innerSegments = segment.Split('#');

                FontSourceIdentifier identifier;

                switch (innerSegments.Length)
                {
                    case 1:
                        {
                            identifier = new FontSourceIdentifier(innerSegments[0].Trim(), null);
                            break;
                        }

                    case 2:
                        {
                            var source = innerSegments[0].StartsWith("/", StringComparison.Ordinal)
                                ? new Uri(innerSegments[0], UriKind.Relative)
                                : new Uri(innerSegments[0], UriKind.RelativeOrAbsolute);

                            identifier = new FontSourceIdentifier(innerSegments[1].Trim(), source);

                            break;
                        }

                    default:
                        {
                            identifier = new FontSourceIdentifier(name, null);
                            break;
                        }
                }

                result.Add(identifier);
            }

            return result;
        }

        /// <summary>
        /// Parses a <see cref="T:Avalonia.Media.FontFamily"/> string.
        /// </summary>
        /// <param name="s">The <see cref="T:Avalonia.Media.FontFamily"/> string.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">
        /// Specified family is not supported.
        /// </exception>
        public static FontFamily Parse(string s) => Parse(s, null);

        /// <summary>
        /// Parses a <see cref="T:Avalonia.Media.FontFamily"/> string.
        /// </summary>
        /// <param name="s">The <see cref="T:Avalonia.Media.FontFamily"/> string.</param>
        /// <param name="baseUri">Specifies the base uri that is used to resolve font family assets.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">
        /// Specified family is not supported.
        /// </exception>
        public static FontFamily Parse(string s, Uri? baseUri)
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
                return (FamilyNames.GetHashCode() * 397) ^ (Key is not null ? Key.GetHashCode() : 0);
            }
        }

        public static bool operator !=(FontFamily? a, FontFamily? b)
        {
            return !(a == b);
        }

        public static bool operator ==(FontFamily? a, FontFamily? b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            return !(a is null) && a.Equals(b);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (!(obj is FontFamily other))
            {
                return false;
            }

            if (!Equals(Key, other.Key))
            {
                return false;
            }

            return other.FamilyNames.Equals(FamilyNames);
        }
    }
}
