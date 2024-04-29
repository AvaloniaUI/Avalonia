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
        /// <param name="source"></param>
        /// <returns></returns>
        public static IEnumerable<Uri> LoadFontAssets(Uri source)
        {
            if (source.IsAvares() || source.IsAbsoluteResm())
            {
                return IsFontTtfOrOtf(source) ?
                    GetFontAssetsByExpression(source) :
                    GetFontAssetsBySource(source);
            }

            return Enumerable.Empty<Uri>();
        }


        /// <summary>
        /// Searches for font assets at a given location and returns a quantity of found assets
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        private static IEnumerable<Uri> GetFontAssetsBySource(Uri source)
        {
            var assetLoader = AvaloniaLocator.Current.GetRequiredService<IAssetLoader>();
            var availableAssets = assetLoader.GetAssets(source, null);
            return availableAssets.Where(x => IsFontTtfOrOtf(x));
        }

        /// <summary>
        /// Searches for font assets at a given location and only accepts assets that fit to a given filename expression.
        /// <para>File names can target multiple files with * wildcard. For example "FontFile*.ttf"</para>
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        private static IEnumerable<Uri> GetFontAssetsByExpression(Uri source)
        {
            var (fileNameWithoutExtension, extension) = GetFileName(source, out var location);
            var filePattern = CreateFilePattern(source, location, fileNameWithoutExtension);

            var assetLoader = AvaloniaLocator.Current.GetRequiredService<IAssetLoader>();
            var availableResources = assetLoader.GetAssets(location, null);

            return availableResources.Where(x => IsContainsFile(x, filePattern, extension));
        }

        private static (string fileNameWithoutExtension, string extension) GetFileName(
            Uri source, out Uri location)
        {
            if (source.IsAbsoluteResm())
            {
                var fileName = GetFileNameAndExtension(source.GetUnescapeAbsolutePath(), '.');

                var uriLocation = source.GetUnescapeAbsoluteUri()
                    .Replace("." + fileName.fileNameWithoutExtension + fileName.extension, string.Empty);
                location = new Uri(uriLocation, UriKind.RelativeOrAbsolute);

                return fileName;
            }

            var filename = GetFileNameAndExtension(source.OriginalString);
            var fullFilename = filename.fileNameWithoutExtension + filename.extension;

            var uriString = source
                .GetUnescapeAbsoluteUri()
                .Replace(fullFilename, string.Empty);
            location = new Uri(uriString);

            return filename;
        }

        private static string CreateFilePattern(
            Uri source, Uri location, string fileNameWithoutExtension)
        {
            var path = location.GetUnescapeAbsolutePath();
            var file = GetSubString(fileNameWithoutExtension, '*');
            return source.IsAbsoluteResm()
                ? path + "." + file
                : path + file;
        }

        private static bool IsContainsFile(Uri x, string filePattern, string fileExtension)
        {
            var path = x.GetUnescapeAbsolutePath();
            return path.IndexOf(filePattern, StringComparison.Ordinal) >= 0
                   && path.EndsWith(fileExtension, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsFontTtfOrOtf(Uri uri)
        {
            var sourceWithoutArguments = GetSubString(uri.OriginalString, '?');
            return sourceWithoutArguments.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase)
                   || sourceWithoutArguments.EndsWith(".otf", StringComparison.OrdinalIgnoreCase);
        }

        private static (string fileNameWithoutExtension, string extension) GetFileNameAndExtension(
            string path, char directorySeparator = '/')
        {
            var pathAsSpan = path.AsSpan();
            pathAsSpan = IsPathRooted(pathAsSpan, directorySeparator) ?
                pathAsSpan.Slice(1, path.Length - 1) :
                pathAsSpan;

            var extension = GetFileExtension(pathAsSpan);
            if (extension.Length == pathAsSpan.Length)
                return (extension.ToString(), string.Empty);

            var fileName = GetFileName(pathAsSpan, directorySeparator, extension.Length);
            return (fileName.ToString(), extension.ToString());
        }

        private static bool IsPathRooted(ReadOnlySpan<char> path, char directorySeparator) =>
            path.Length > 0 && path[0] == directorySeparator;

        private static ReadOnlySpan<char> GetFileExtension(ReadOnlySpan<char> path)
        {
            for (var i = path.Length - 1; i > 0; --i)
            {
                if (path[i] == '.')
                    return path.Slice(i, path.Length - i);
            }

            return path;
        }

        private static ReadOnlySpan<char> GetFileName(ReadOnlySpan<char> path, char directorySeparator, int extensionLength)
        {
            for (var i = path.Length - extensionLength - 1; i >= 0; --i)
            {
                if (path[i] == directorySeparator)
                    return path.Slice(i + 1, path.Length - i - extensionLength - 1);
            }

            return path.Slice(0, path.Length - extensionLength);
        }

        private static string GetSubString(string path, char separator)
        {
            for (var i = 0; i < path.Length; i++)
            {
                if (path[i] == separator)
                    return path.Substring(0, i);
            }

            return path;
        }
    }
}
