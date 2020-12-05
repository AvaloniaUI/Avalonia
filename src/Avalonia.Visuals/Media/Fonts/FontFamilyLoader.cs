using System;
using System.Collections.Generic;
using System.IO;
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
            var fileName = GetFileName(fontFamilyKey, out var location);
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);

            var filePattern = CreateFilePattern(fontFamilyKey, location, fileNameWithoutExtension);
            var fileExtension = Path.GetExtension(fileName);

            var assetLoader = AvaloniaLocator.Current.GetService<IAssetLoader>();
            var availableResources = assetLoader.GetAssets(location, fontFamilyKey.BaseUri);
            return availableResources.Where(x => IsContainsFile(x, filePattern, fileExtension));
        }

        private static string GetFileName(FontFamilyKey fontFamilyKey, out Uri location)
        {
            if (fontFamilyKey.Source.IsAbsoluteResm())
            {
                var fileName = Path.GetFileName(fontFamilyKey.Source.GetUnescapeAbsolutePath());

                location = new Uri(
                    fontFamilyKey.Source.AbsoluteUri.Replace("." + fileName, string.Empty),
                    UriKind.RelativeOrAbsolute);

                return fileName;
            }

            var filename = Path.GetFileName(fontFamilyKey.Source.OriginalString);

            if (fontFamilyKey.BaseUri != null)
            {
                var relativePath = fontFamilyKey.Source.OriginalString
                    .Replace(filename, string.Empty);

                location = new Uri(fontFamilyKey.BaseUri, relativePath);
            }
            else
            {
                location = new Uri(
                    fontFamilyKey.Source
                        .GetUnescapeAbsolutePath()
                        .Replace(filename, string.Empty));
            }

            return filename;
        }

        private static bool IsFontTtfOrOtf(Uri uri)
        {
            var sourceWithoutArguments = uri.OriginalString.Split('?')[0];
            return sourceWithoutArguments.EndsWith(".ttf", StringComparison.Ordinal)
                   || sourceWithoutArguments.EndsWith(".otf", StringComparison.Ordinal);
        }

        private static string CreateFilePattern(
            FontFamilyKey fontFamilyKey, Uri location, string fileNameWithoutExtension)
        {
            var path = location.GetUnescapeAbsolutePath();
            var file = fileNameWithoutExtension.Split('*').First();
            return fontFamilyKey.Source.IsAbsoluteResm()
                ? path + "." + file
                : path + file;
        }

        private static bool IsContainsFile(Uri x, string filePattern, string fileExtension)
        {
            var path = x.GetUnescapeAbsolutePath();
            return path.Contains(filePattern) && path.EndsWith(fileExtension);
        }
    }
}
