using System;
using Avalonia.Platform;

namespace Avalonia.Media.Fonts
{
    public class EmbeddedFontCollection : FontCollectionBase
    {
        private readonly Uri _key;

        private readonly Uri _source;

        public EmbeddedFontCollection(Uri key, Uri source)
        {
            _key = key;

            _source = source;
        }

        public override Uri Key => _key;

        public override void Initialize(IFontManagerImpl fontManager)
        {
            var assetLoader = AvaloniaLocator.Current.GetRequiredService<IAssetLoader>();

            var fontAssets = FontFamilyLoader.LoadFontAssets(_source);

            foreach (var fontAsset in fontAssets)
            {
                var stream = assetLoader.Open(fontAsset);

                if (fontManager.TryCreateGlyphTypeface(stream, FontSimulations.None, out var glyphTypeface))
                {
                    TryAddGlyphTypeface(glyphTypeface);
                }
            }
        }
    }
}
