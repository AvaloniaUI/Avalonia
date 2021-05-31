using System.Collections.Generic;
using Avalonia.Platform;
using Vortice.DirectWrite;
using System;
using SharpGen.Runtime;
using Vortice;

namespace Avalonia.Direct2D1.Media
{
    internal class DWriteResourceFontLoader : CallbackBase, IDWriteFontCollectionLoader, IDWriteFontFileLoader
    {
        private readonly List<DataStream> _fontStreams = new List<DataStream>();
        private readonly DataStream _keyStream;

        /// <summary>
        /// Initializes a new instance of the <see cref="DWriteResourceFontLoader"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="fontAssets"></param>
        public DWriteResourceFontLoader(IDWriteFactory factory, IEnumerable<Uri> fontAssets)
        {
            var factory1 = factory;

            var assetLoader = AvaloniaLocator.Current.GetService<IAssetLoader>();

            foreach (var asset in fontAssets)
            {
                var assetStream = assetLoader.Open(asset);

                var dataStream = new DataStream((int)assetStream.Length, true, true);

                assetStream.CopyTo(dataStream);

                dataStream.Position = 0;

                _fontStreams.Add(dataStream);
            }

            // Build a Key storage that stores the index of the font
            _keyStream = new DataStream(sizeof(int) * _fontStreams.Count, true, true);

            for (int i = 0; i < _fontStreams.Count; i++)
            {
                _keyStream.Write(i);
            }

            _keyStream.Position = 0;

            // Register the 
            factory1.RegisterFontFileLoader(this);
            factory1.RegisterFontCollectionLoader(this);
        }

        /// <summary>
        /// Gets the key used to identify the FontCollection as well as storing index for fonts.
        /// </summary>
        /// <value>The key.</value>
        public DataStream Key => _keyStream;

        /// <summary>
        /// Creates a font file enumerator object that encapsulates a collection of font files. The font system calls back to this interface to create a font collection.
        /// </summary>
        /// <param name="factory">Pointer to the <see cref="IDWriteFactory"/> object that was used to create the current font collection.</param>
        /// <param name="collectionKey">A font collection key that uniquely identifies the collection of font files within the scope of the font collection loader being used. The buffer allocated for this key must be at least  the size, in bytes, specified by collectionKeySize.</param>
        /// <param name="collectionKeySize"></param>
        /// <unmanaged>HRESULT IDWriteFontCollectionLoader::CreateEnumeratorFromKey([None] IDWriteFactory* factory,[In, Buffer] const void* collectionKey,[None] int collectionKeySize,[Out] IDWriteFontFileEnumerator** fontFileEnumerator)</unmanaged>
        public IDWriteFontFileEnumerator CreateEnumeratorFromKey(IDWriteFactory factory, IntPtr collectionKey, int collectionKeySize)
        {
            return new DWriteResourceFontFileEnumerator(factory, this, collectionKey, collectionKeySize);
        }

        /// <summary>
        /// Creates a font file stream object that encapsulates an open file resource.
        /// </summary>
        /// <param name="fontFileReferenceKey">A reference to a font file reference key that uniquely identifies the font file resource within the scope of the font loader being used. The buffer allocated for this key must at least be the size, in bytes, specified by  fontFileReferenceKeySize.</param>
        /// <returns>
        /// a reference to the newly created <see cref="IDWriteFontFileStream"/> object.
        /// </returns>
        /// <remarks>
        /// The resource is closed when the last reference to fontFileStream is released.
        /// </remarks>
        /// <unmanaged>HRESULT IDWriteFontFileLoader::CreateStreamFromKey([In, Buffer] const void* fontFileReferenceKey,[None] int fontFileReferenceKeySize,[Out] IDWriteFontFileStream** fontFileStream)</unmanaged>
        public IDWriteFontFileStream CreateStreamFromKey(IntPtr fontFileReferenceKey, int fontFileReferenceKeySize)
        {
            int index = 0;
            MemoryHelpers.Read(fontFileReferenceKey, ref index);

            return new DWriteResourceFontFileStream(_fontStreams[index]);
        }
    }
}
