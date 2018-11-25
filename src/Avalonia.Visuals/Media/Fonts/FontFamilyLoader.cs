// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        public static IEnumerable<Uri> LoadFontAssets(FontFamilyKey fontFamilyKey)
        {
            return fontFamilyKey.FileName != null
                ? GetFontAssetsByFileName(fontFamilyKey.Location, fontFamilyKey.FileName)
                : GetFontAssetsByLocation(fontFamilyKey.Location);
        }

        /// <summary>
        /// Searches for font assets at a given location and returns a quantity of found assets
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        private static IEnumerable<Uri> GetFontAssetsByLocation(Uri location)
        {
            var availableAssets = s_assetLoader.GetAssets(location, null);

            var matchingAssets = availableAssets.Where(x => x.AbsolutePath.EndsWith(".ttf") || x.AbsolutePath.EndsWith(".otf"));

            return matchingAssets;
        }

        /// <summary>
        /// Searches for font assets at a given location and only accepts assets that fit to a given filename expression.
        /// <para>File names can target multiple files with * wildcard. For example "FontFile*.ttf"</para>
        /// </summary>
        /// <param name="location"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private static IEnumerable<Uri> GetFontAssetsByFileName(Uri location, string fileName)
        {
            var availableResources = s_assetLoader.GetAssets(location, null);

            var compareTo = location.AbsolutePath + "." + fileName.Split('*').First();

            var matchingResources =
                availableResources.Where(x => x.AbsolutePath.Contains(compareTo) && (x.AbsolutePath.EndsWith(".ttf") || x.AbsolutePath.EndsWith(".otf")));

            return matchingResources;
        }

    }
}
