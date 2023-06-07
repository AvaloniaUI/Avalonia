using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Avalonia.Platform;

#if !BUILDTASK
/// <inheritdoc cref="IAssetLoader"/>
#endif
public static class AssetLoader
{
#if !BUILDTASK
    private static IAssetLoader GetAssetLoader() => AvaloniaLocator.Current.GetRequiredService<IAssetLoader>();

    /// <inheritdoc cref="IAssetLoader.SetDefaultAssembly"/>
    public static void SetDefaultAssembly(Assembly assembly) => GetAssetLoader().SetDefaultAssembly(assembly);

    /// <inheritdoc cref="IAssetLoader.Exists"/>
    public static bool Exists(Uri uri, Uri? baseUri = null) => GetAssetLoader().Exists(uri, baseUri);

    /// <inheritdoc cref="IAssetLoader.Open"/>
    public static Stream Open(Uri uri, Uri? baseUri = null) => GetAssetLoader().Open(uri, baseUri);

    /// <inheritdoc cref="IAssetLoader.OpenAndGetAssembly"/>
    public static (Stream stream, Assembly assembly) OpenAndGetAssembly(Uri uri, Uri? baseUri = null)
        => GetAssetLoader().OpenAndGetAssembly(uri, baseUri);

    /// <inheritdoc cref="IAssetLoader.GetAssembly"/>
    public static Assembly? GetAssembly(Uri uri, Uri? baseUri = null)
        => GetAssetLoader().GetAssembly(uri, baseUri);

    /// <inheritdoc cref="IAssetLoader.GetAssets"/>
    public static IEnumerable<Uri> GetAssets(Uri uri, Uri? baseUri)
        => GetAssetLoader().GetAssets(uri, baseUri);
#endif

    internal static void RegisterResUriParsers()
    {
        if (!UriParser.IsKnownScheme("avares"))
            UriParser.Register(new GenericUriParser(
                GenericUriParserOptions.GenericAuthority |
                GenericUriParserOptions.NoUserInfo |
                GenericUriParserOptions.NoPort |
                GenericUriParserOptions.NoQuery |
                GenericUriParserOptions.NoFragment), "avares", -1);
    }
}
