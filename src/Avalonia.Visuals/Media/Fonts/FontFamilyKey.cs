// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;

namespace Avalonia.Media.Fonts
{
    /// <summary>
    /// Unique idetifier for a quantity of <see cref="FontResource"/> that is stored at a given location.
    /// </summary>
    public class FontFamilyKey
    {
        /// <summary>
        /// Creates a new instance of <see cref="FontFamilyKey"/> and extracts <see cref="Location"/> and <see cref="FileName"/> from given <see cref="Uri"/>
        /// </summary>
        /// <param name="source"></param>
        public FontFamilyKey(Uri source)
        {
            if (source.AbsolutePath.Contains(".ttf"))
            {
                if (source.Scheme == "res")
                {
                    FileName = source.AbsolutePath.Split('/').Last();
                    Location = new Uri(source.OriginalString.Replace("/" + FileName, ""), UriKind.RelativeOrAbsolute);
                }
                else
                {
                    var filePathWithoutExtension = source.AbsolutePath.Replace(".ttf", "");
                    var fileNameWithoutExtension = filePathWithoutExtension.Split('.').Last();
                    FileName = fileNameWithoutExtension + ".ttf";
                    Location = new Uri(source.OriginalString.Replace("." + FileName, ""), UriKind.RelativeOrAbsolute);
                }            
            }
            else
            {
                Location = source;
            }
        }

        /// <summary>
        /// Location of stored <see cref="FontResource"/> that belong to a <see cref="FontFamily"/>
        /// </summary>
        public Uri Location { get; }

        /// <summary>
        /// Optional filename for <see cref="FontResource"/> that belong to a <see cref="FontFamily"/>
        /// </summary>
        public string FileName { get; }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)2166136261;

                if (Location != null)
                {
                    hash = (hash * 16777619) ^ Location.GetHashCode();
                }

                if (FileName != null)
                {
                    hash = (hash * 16777619) ^ FileName.GetHashCode();
                }

                return hash;
            }
        }

        /// <summary>
        /// Determines whether the specified <see cref="object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (!(obj is FontFamilyKey other)) return false;

            if (Location != other.Location) return false;

            if (FileName != other.FileName) return false;

            return true;
        }

        /// <summary>
        /// Returns a <see cref="string" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            if (FileName != null)
            {
                if (Location.Scheme == "resm")
                {
                    return Location.AbsolutePath + "." + FileName;
                }

                return Location.AbsolutePath + "/" + FileName;
            }

            return Location.AbsolutePath;
        }
    }
}