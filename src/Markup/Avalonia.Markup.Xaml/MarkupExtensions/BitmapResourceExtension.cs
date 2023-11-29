using System;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Avalonia.Markup.Xaml.MarkupExtensions;

/// <summary>
/// Reads a file or an asset and loads it as a <see cref="Bitmap"/>. Supports absolute file URIs plus all the URI schemes of <see cref="AssetLoader"/>.
/// </summary>
public class BitmapResourceExtension
{
    public BitmapResourceExtension() { }

    /// <inheritdoc cref="BitmapResourceExtension"/>
    public BitmapResourceExtension(Uri? source)
    {
        Source = source;
    }

    public Uri? Source { get; set; }

    public Bitmap? ProvideValue(IServiceProvider serviceProvider) => Source switch
    {
        { IsAbsoluteUri: true, IsFile: true, LocalPath: { } localPath } => new Bitmap(localPath),
        { } uri => new Bitmap(AssetLoader.Open(uri, serviceProvider.GetContextBaseUri())),
        _ => null,
    };
}
