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
            base.Initialize(fontManager);

            TryAddFontSource(_source);
        }
    }
}
