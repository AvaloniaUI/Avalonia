using System.Collections.Generic;
using SharpDX;
using SharpDX.DirectWrite;

namespace Avalonia.Direct2D1.Media
{
    using System.IO;

    internal class DWriteResourceFontLoader : CallbackBase, FontCollectionLoader, FontFileLoader
    {
        private readonly List<DWriteResourceFontFileStream> _fontStreams = new List<DWriteResourceFontFileStream>();
        private readonly List<DWriteResourceFontFileEnumerator> _enumerators = new List<DWriteResourceFontFileEnumerator>();
        private readonly DataStream _keyStream;

        /// <summary>
        /// Initializes a new instance of the <see cref="DWriteResourceFontLoader"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="fontAssets"></param>
        public DWriteResourceFontLoader(Factory factory, Stream[] fontAssets)
        {
            var factory1 = factory;

            foreach (var asset in fontAssets)
            {
                var dataStream = new DataStream((int)asset.Length, true, true);

                asset.CopyTo(dataStream);

                dataStream.Position = 0;

                _fontStreams.Add(new DWriteResourceFontFileStream(dataStream));
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
        /// <param name="factory">Pointer to the <see cref="SharpDX.DirectWrite.Factory"/> object that was used to create the current font collection.</param>
        /// <param name="collectionKey">A font collection key that uniquely identifies the collection of font files within the scope of the font collection loader being used. The buffer allocated for this key must be at least  the size, in bytes, specified by collectionKeySize.</param>
        /// <returns>
        /// a reference to the newly created font file enumerator.
        /// </returns>
        /// <unmanaged>HRESULT IDWriteFontCollectionLoader::CreateEnumeratorFromKey([None] IDWriteFactory* factory,[In, Buffer] const void* collectionKey,[None] int collectionKeySize,[Out] IDWriteFontFileEnumerator** fontFileEnumerator)</unmanaged>
        FontFileEnumerator FontCollectionLoader.CreateEnumeratorFromKey(Factory factory, DataPointer collectionKey)
        {
            var enumerator = new DWriteResourceFontFileEnumerator(factory, this, collectionKey);

            _enumerators.Add(enumerator);

            return enumerator;
        }

        /// <summary>
        /// Creates a font file stream object that encapsulates an open file resource.
        /// </summary>
        /// <param name="fontFileReferenceKey">A reference to a font file reference key that uniquely identifies the font file resource within the scope of the font loader being used. The buffer allocated for this key must at least be the size, in bytes, specified by  fontFileReferenceKeySize.</param>
        /// <returns>
        /// a reference to the newly created <see cref="SharpDX.DirectWrite.FontFileStream"/> object.
        /// </returns>
        /// <remarks>
        /// The resource is closed when the last reference to fontFileStream is released.
        /// </remarks>
        /// <unmanaged>HRESULT IDWriteFontFileLoader::CreateStreamFromKey([In, Buffer] const void* fontFileReferenceKey,[None] int fontFileReferenceKeySize,[Out] IDWriteFontFileStream** fontFileStream)</unmanaged>
        FontFileStream FontFileLoader.CreateStreamFromKey(DataPointer fontFileReferenceKey)
        {
            var index = SharpDX.Utilities.Read<int>(fontFileReferenceKey.Pointer);

            return _fontStreams[index];
        }
    }
}
