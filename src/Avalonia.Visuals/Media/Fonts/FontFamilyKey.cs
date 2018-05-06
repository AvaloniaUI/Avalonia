// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;

namespace Avalonia.Media.Fonts
{
    public class FontFamilyKey
    {
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

        public Uri Location { get; }

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

        public override bool Equals(object obj)
        {
            if (!(obj is FontFamilyKey other)) return false;

            if (Location != other.Location) return false;

            if (FileName != other.FileName) return false;

            return true;
        }

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