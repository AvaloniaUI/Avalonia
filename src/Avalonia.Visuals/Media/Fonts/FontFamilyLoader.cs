// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Platform;

namespace Avalonia.Media.Fonts
{
    public static class FontFamilyLoader
    {
        private static readonly IAssetLoader s_assetLoader;

        static FontFamilyLoader()
        {
            s_assetLoader = AvaloniaLocator.Current.GetService<IAssetLoader>();
        }

        /// <summary>
        /// Loads all font assets that belong to the specified <see cref="FontFamilyKey"/>
        /// </summary>
        /// <param name="fontFamilyKey"></param>
        /// <returns></returns>
        public static IEnumerable<Uri> LoadFontAssets(FontFamilyKey fontFamilyKey)
        {
            var sourceWithoutArguments = fontFamilyKey.Source.OriginalString.Split('?').First();

            if (sourceWithoutArguments.EndsWith(".ttf")
                || sourceWithoutArguments.EndsWith(".otf"))
            {
                return GetFontAssetsByExpression(fontFamilyKey);
            }

            return GetFontAssetsBySource(fontFamilyKey);
        }

        /// <summary>
        /// Searches for font assets at a given location and returns a quantity of found assets
        /// </summary>
        /// <param name="fontFamilyKey"></param>
        /// <returns></returns>
        private static IEnumerable<Uri> GetFontAssetsBySource(FontFamilyKey fontFamilyKey)
        {
            var availableAssets = s_assetLoader.GetAssets(fontFamilyKey.Source, fontFamilyKey.BaseUri);

            var matchingAssets =
                availableAssets.Where(x => x.AbsolutePath.EndsWith(".ttf") || x.AbsolutePath.EndsWith(".otf"));

            return matchingAssets;
        }

        /// <summary>
        /// Searches for font assets at a given location and only accepts assets that fit to a given filename expression.
        /// <para>File names can target multiple files with * wildcard. For example "FontFile*.ttf"</para>
        /// </summary>
        /// <param name="fontFamilyKey"></param>
        /// <returns></returns>
        private static IEnumerable<Uri> GetFontAssetsByExpression(FontFamilyKey fontFamilyKey)
        {
            var fileName = GetFileName(fontFamilyKey, out var fileExtension, out var location);

            var availableResources = s_assetLoader.GetAssets(location, fontFamilyKey.BaseUri);

            string compareTo;

            if (fontFamilyKey.Source.IsAbsoluteUri)
            {
                if (fontFamilyKey.Source.Scheme == "resm")
                {
                    compareTo = location.AbsolutePath + "." + fileName.Split('*').First();
                }
                else
                {
                    compareTo = location.AbsolutePath + fileName.Split('*').First();
                }
            }
            else
            {
                compareTo = location.AbsolutePath + fileName.Split('*').First();
            }

            var matchingResources = availableResources.Where(
                x => x.AbsolutePath.Contains(compareTo)
                     && x.AbsolutePath.EndsWith(fileExtension));

            return matchingResources;
        }

        private static string GetFileName(FontFamilyKey fontFamilyKey, out string fileExtension, out Uri location)
        {
            if (fontFamilyKey.Source.IsAbsoluteUri && fontFamilyKey.Source.Scheme == "resm")
            {
                fileExtension = "." + fontFamilyKey.Source.AbsolutePath.Split('.').LastOrDefault();

                var fileName = fontFamilyKey.Source.LocalPath.Replace(fileExtension, string.Empty).Split('.').LastOrDefault();

                location = new Uri(fontFamilyKey.Source.AbsoluteUri.Replace("." + fileName + fileExtension, string.Empty), UriKind.RelativeOrAbsolute);

                return fileName;
            }

            var pathSegments = fontFamilyKey.Source.OriginalString.Split('/');

            var fileNameWithExtension = pathSegments.Last();

            var fileNameSegments = fileNameWithExtension.Split('.');

            fileExtension = "." + fileNameSegments.Last();

            if (fontFamilyKey.BaseUri != null)
            {
                var relativePath = fontFamilyKey.Source.OriginalString
                    .Replace(fileNameWithExtension, string.Empty);

                location = new Uri(fontFamilyKey.BaseUri, relativePath);
            }
            else
            {
                location = new Uri(fontFamilyKey.Source.AbsolutePath.Replace(fileNameWithExtension, string.Empty));
            }

            return fileNameSegments.First();
        }
    }
}
