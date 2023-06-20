using SharpDX;
using SharpDX.DirectWrite;

namespace Avalonia.Direct2D1.Media
{
    /// <summary>
    /// Resource FontFileEnumerator.
    /// </summary>
    internal class DWriteResourceFontFileEnumerator : CallbackBase, FontFileEnumerator
    {
        private readonly Factory _factory;
        private readonly FontFileLoader _loader;
        private readonly DataStream _keyStream;
        private FontFile _currentFontFile;

        /// <summary>
        /// Initializes a new instance of the <see cref="DWriteResourceFontFileEnumerator"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="loader">The loader.</param>
        /// <param name="key">The key.</param>
        public DWriteResourceFontFileEnumerator(Factory factory, FontFileLoader loader, DataPointer key)
        {
            _factory = factory;
            _loader = loader;
            _keyStream = new DataStream(key.Pointer, key.Size, true, false);
        }

        /// <summary>
        /// Advances to the next font file in the collection. When it is first created, the enumerator is positioned before the first element of the collection and the first call to MoveNext advances to the first file.
        /// </summary>
        /// <returns>
        /// the value TRUE if the enumerator advances to a file; otherwise, FALSE if the enumerator advances past the last file in the collection.
        /// </returns>
        /// <unmanaged>HRESULT IDWriteFontFileEnumerator::MoveNext([Out] BOOL* hasCurrentFile)</unmanaged>
        bool FontFileEnumerator.MoveNext()
        {
            bool moveNext = _keyStream.RemainingLength != 0;

            if (!moveNext) return false;

            _currentFontFile?.Dispose();

            _currentFontFile = new FontFile(_factory, _keyStream.PositionPointer, 4, _loader);

            _keyStream.Position += 4;

            return true;
        }

        /// <summary>
        /// Gets a reference to the current font file.
        /// </summary>
        /// <value></value>
        /// <returns>a reference to the newly created <see cref="SharpDX.DirectWrite.FontFile"/> object.</returns>
        /// <unmanaged>HRESULT IDWriteFontFileEnumerator::GetCurrentFontFile([Out] IDWriteFontFile** fontFile)</unmanaged>
        FontFile FontFileEnumerator.CurrentFontFile
        {
            get
            {
                ((IUnknown)_currentFontFile).AddReference();

                return _currentFontFile;
            }
        }
    }
}
