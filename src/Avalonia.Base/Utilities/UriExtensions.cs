using System;
using System.Collections.Generic;
using System.Linq;

namespace Avalonia.Utilities
{
    internal static class UriExtensions
    {
        public static bool IsAbsoluteResm(this Uri uri) =>
            uri.IsAbsoluteUri && uri.IsResm();

        public static bool IsResm(this Uri uri) => uri.Scheme == "resm";
        
        public static bool IsAvares(this Uri uri) => uri.Scheme == "avares";
        
        public static Uri EnsureAbsolute(this Uri uri, Uri baseUri)
        {
            if (uri.IsAbsoluteUri)
                return uri;
            if(baseUri == null)
                throw new ArgumentException($"Relative uri {uri} without base url");
            if (!baseUri.IsAbsoluteUri)
                throw new ArgumentException($"Base uri {baseUri} is relative");
            if (baseUri.IsResm())
                throw new ArgumentException(
                    $"Relative uris for 'resm' scheme aren't supported; {baseUri} uses resm");
            return new Uri(baseUri, uri);
        }

        public static string GetUnescapeAbsolutePath(this Uri uri) =>
            Uri.UnescapeDataString(uri.AbsolutePath);

        public static string GetUnescapeAbsoluteUri(this Uri uri) =>
            Uri.UnescapeDataString(uri.AbsoluteUri);
        
        public static Dictionary<string, string> ParseQueryString(this Uri uri) =>
            Uri.UnescapeDataString(uri.Query)
                .TrimStart('?')
                .Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Split('='))
                .ToDictionary(p => p[0], p => p[1]);
    }
}
