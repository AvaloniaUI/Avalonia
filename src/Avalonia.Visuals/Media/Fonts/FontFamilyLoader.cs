using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Platform;
using Avalonia.Utilities;

namespace Avalonia.Media.Fonts
{
    public static class FontFamilyLoader
    {
        /// <summary>
        /// Loads all font assets that belong to the specified <see cref="FontFamilyKey"/>
        /// </summary>
        /// <param name="fontFamilyKey"></param>
        /// <returns></returns>
        public static IEnumerable<Uri> LoadFontAssets(FontFamilyKey fontFamilyKey) =>
            IsFontTtfOrOtf(fontFamilyKey.Source)
                ? GetFontAssetsByExpression(fontFamilyKey)
                : GetFontAssetsBySource(fontFamilyKey);

        /// <summary>
        /// Searches for font assets at a given location and returns a quantity of found assets
        /// </summary>
        /// <param name="fontFamilyKey"></param>
        /// <returns></returns>
        private static IEnumerable<Uri> GetFontAssetsBySource(FontFamilyKey fontFamilyKey)
        {
            var assetLoader = AvaloniaLocator.Current.GetService<IAssetLoader>();
            var availableAssets = assetLoader.GetAssets(fontFamilyKey.Source, fontFamilyKey.BaseUri);
            return availableAssets.Where(IsFontTtfOrOtf);
        }

        /// <summary>
        /// Searches for font assets at a given location and only accepts assets that fit to a given filename expression.
        /// <para>File names can target multiple files with * wildcard. For example "FontFile*.ttf"</para>
        /// </summary>
        /// <param name="fontFamilyKey"></param>
        /// <returns></returns>
        private static IEnumerable<Uri> GetFontAssetsByExpression(FontFamilyKey fontFamilyKey)
        {
            var assetLoader = AvaloniaLocator.Current.GetService<IAssetLoader>();

            var fileName = GetFileName(fontFamilyKey, out var fileExtension, out var location);

            var availableResources = assetLoader.GetAssets(location, fontFamilyKey.BaseUri);

            string compareTo;

            if (fontFamilyKey.Source.IsAbsoluteUri)
            {
                if (fontFamilyKey.Source.IsResm())
                {
                    compareTo = location.GetUnescapeAbsolutePath() + "." + fileName.Split('*').First();
                }
                else
                {
                    compareTo = location.GetUnescapeAbsolutePath() + fileName.Split('*').First();
                }
            }
            else
            {
                compareTo = location.GetUnescapeAbsolutePath() + fileName.Split('*').First();
            }

            var matchingResources = availableResources.Where(
                x => x.GetUnescapeAbsolutePath().Contains(compareTo)
                     && x.GetUnescapeAbsolutePath().EndsWith(fileExtension));

            return matchingResources;
        }

        private static string GetFileName(FontFamilyKey fontFamilyKey, out string fileExtension, out Uri location)
        {
            if (fontFamilyKey.Source.IsAbsoluteResm())
            {
                fileExtension = "." + fontFamilyKey.Source.GetUnescapeAbsolutePath().Split('.').LastOrDefault();

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
                location = new Uri(
                    fontFamilyKey.Source
                        .GetUnescapeAbsolutePath()
                        .Replace(fileNameWithExtension, string.Empty));
            }

            return fileNameSegments.First();
        }

        private static bool IsFontTtfOrOtf(Uri uri)
        {
            var sourceWithoutArguments = uri.OriginalString.Split('?')[0];
            return sourceWithoutArguments.EndsWith(".ttf", StringComparison.Ordinal)
                   || sourceWithoutArguments.EndsWith(".otf", StringComparison.Ordinal);
        }
    }
}
