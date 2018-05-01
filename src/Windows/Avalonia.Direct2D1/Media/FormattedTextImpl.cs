// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using Avalonia.Platform;
using SharpDX;
using DWrite = SharpDX.DirectWrite;

namespace Avalonia.Direct2D1.Media
{
    public class FormattedTextImpl : IFormattedTextImpl
    {
        public FormattedTextImpl(
            string text,
            Typeface typeface,
            TextAlignment textAlignment,
            TextWrapping wrapping,
            Size constraint,
            IReadOnlyList<FormattedTextStyleSpan> spans)
        {
            Text = text;

            var factory = AvaloniaLocator.Current.GetService<DWrite.Factory>();

            DWrite.TextFormat textFormat;

            if (typeface.FontFamily.BaseUri != null)
            {
                var fontCollection = Direct2D1CustomFontResourceCache.GetOrAddCustomFontResource(typeface.FontFamily, factory);

                textFormat = new DWrite.TextFormat(
                        factory,
                        typeface.FontFamily.Name,
                        fontCollection,
                        (DWrite.FontWeight)typeface.Weight,
                        (DWrite.FontStyle)typeface.Style,
                        DWrite.FontStretch.Normal,
                        (float)typeface.FontSize);
            }
            else
            {
                textFormat = new DWrite.TextFormat(
                    factory,
                    typeface.FontFamily.Name,
                    (DWrite.FontWeight)typeface.Weight,
                    (DWrite.FontStyle)typeface.Style,
                    (float)typeface.FontSize);
            }

            textFormat.TextAlignment = DWrite.TextAlignment.Center;
            textFormat.ParagraphAlignment = DWrite.ParagraphAlignment.Center;

            textFormat.WordWrapping = wrapping == TextWrapping.Wrap ?
                DWrite.WordWrapping.Wrap :
                DWrite.WordWrapping.NoWrap;

            TextLayout = new DWrite.TextLayout(factory, Text ?? string.Empty, textFormat, (float)constraint.Width,
                (float)constraint.Height)
            {
                TextAlignment = textAlignment.ToDirect2D()
            };

            textFormat.Dispose();

            if (spans != null)
            {
                foreach (var span in spans)
                {
                    ApplySpan(span);
                }
            }

            Size = Measure();
        }

        public Size Constraint => new Size(TextLayout.MaxWidth, TextLayout.MaxHeight);

        public Size Size { get; }

        public string Text { get; }

        public DWrite.TextLayout TextLayout { get; }

        //public void Dispose()
        //{
        //    TextLayout.Dispose();
        //}

        public IEnumerable<FormattedTextLine> GetLines()
        {
            var result = TextLayout.GetLineMetrics();
            return from line in result select new FormattedTextLine(line.Length, line.Height);
        }

        public TextHitTestResult HitTestPoint(Point point)
        {
            var result = TextLayout.HitTestPoint(
                (float)point.X,
                (float)point.Y,
                out var isTrailingHit,
                out var isInside);

            return new TextHitTestResult
            {
                IsInside = isInside,
                TextPosition = result.TextPosition,
                IsTrailing = isTrailingHit,
            };
        }

        public Rect HitTestTextPosition(int index)
        {
            var result = TextLayout.HitTestTextPosition(index, false, out _, out _);

            return new Rect(result.Left, result.Top, result.Width, result.Height);
        }

        public IEnumerable<Rect> HitTestTextRange(int index, int length)
        {
            var result = TextLayout.HitTestTextRange(index, length, 0, 0);
            return result.Select(x => new Rect(x.Left, x.Top, x.Width, x.Height));
        }

        private void ApplySpan(FormattedTextStyleSpan span)
        {
            if (span.Length > 0)
            {
                if (span.ForegroundBrush != null)
                {
                    TextLayout.SetDrawingEffect(
                        new BrushWrapper(span.ForegroundBrush),
                        new DWrite.TextRange(span.StartIndex, span.Length));
                }
            }
        }

        private Size Measure()
        {
            var metrics = TextLayout.Metrics;
            var width = metrics.WidthIncludingTrailingWhitespace;

            if (float.IsNaN(width))
            {
                width = metrics.Width;
            }

            return new Size(width, TextLayout.Metrics.Height);
        }
    }

    internal static class Direct2D1CustomFontResourceCache
    {
        private static readonly ConcurrentDictionary<FontFamilyKey, DWrite.FontCollection> s_cachedFonts =
            new ConcurrentDictionary<FontFamilyKey, DWrite.FontCollection>();

        public static DWrite.FontCollection GetOrAddCustomFontResource(FontFamily fontFamily, DWrite.Factory factory)
        {
            return s_cachedFonts.GetOrAdd(fontFamily.Key, x => CreateCustomFontResource(x, factory));
        }

        private static DWrite.FontCollection CreateCustomFontResource(FontFamilyKey fontFamilyKey, DWrite.Factory factory)
        {
            var fontLoader = new ResourceFontLoader(factory, fontFamilyKey.BaseUri);

            return new DWrite.FontCollection(factory, fontLoader, fontLoader.Key);
        }
    }

    public class ResourceFontLoader : CallbackBase, DWrite.FontCollectionLoader, DWrite.FontFileLoader
    {
        private readonly List<ResourceFontFileStream> _fontStreams = new List<ResourceFontFileStream>();
        private readonly List<ResourceFontFileEnumerator> _enumerators = new List<ResourceFontFileEnumerator>();
        private readonly DataStream _keyStream;


        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceFontLoader"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="fontResource"></param>
        public ResourceFontLoader(DWrite.Factory factory, Uri fontResource)
        {
            var factory1 = factory;

            var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();

            var resourceStream = assets.Open(fontResource);

            var dataStream = new DataStream((int)resourceStream.Length, true, true);

            resourceStream.CopyTo(dataStream);

            dataStream.Position = 0;

            _fontStreams.Add(new ResourceFontFileStream(dataStream));

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
        DWrite.FontFileEnumerator DWrite.FontCollectionLoader.CreateEnumeratorFromKey(DWrite.Factory factory, DataPointer collectionKey)
        {
            var enumerator = new ResourceFontFileEnumerator(factory, this, collectionKey);

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
        DWrite.FontFileStream DWrite.FontFileLoader.CreateStreamFromKey(DataPointer fontFileReferenceKey)
        {
            var index = SharpDX.Utilities.Read<int>(fontFileReferenceKey.Pointer);

            return _fontStreams[index];
        }
    }

    /// <summary>
    /// This FontFileStream implem is reading data from a <see cref="DataStream"/>.
    /// </summary>
    public class ResourceFontFileStream : CallbackBase, DWrite.FontFileStream
    {
        private readonly DataStream _stream;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceFontFileStream"/> class.
        /// </summary>
        /// <param name="stream">The stream.</param>
        public ResourceFontFileStream(DataStream stream)
        {
            _stream = stream;
        }

        /// <summary>
        /// Reads a fragment from a font file.
        /// </summary>
        /// <param name="fragmentStart">When this method returns, contains an address of a  reference to the start of the font file fragment.  This parameter is passed uninitialized.</param>
        /// <param name="fileOffset">The offset of the fragment, in bytes, from the beginning of the font file.</param>
        /// <param name="fragmentSize">The size of the file fragment, in bytes.</param>
        /// <param name="fragmentContext">When this method returns, contains the address of</param>
        /// <remarks>
        /// Note that ReadFileFragment implementations must check whether the requested font file fragment is within the file bounds. Otherwise, an error should be returned from ReadFileFragment.   {{DirectWrite}} may invoke <see cref="SharpDX.DirectWrite.FontFileStream"/> methods on the same object from multiple threads simultaneously. Therefore, ReadFileFragment implementations that rely on internal mutable state must serialize access to such state across multiple threads. For example, an implementation that uses separate Seek and Read operations to read a file fragment must place the code block containing Seek and Read calls under a lock or a critical section.
        /// </remarks>
        /// <unmanaged>HRESULT IDWriteFontFileStream::ReadFileFragment([Out, Buffer] const void** fragmentStart,[None] __int64 fileOffset,[None] __int64 fragmentSize,[Out] void** fragmentContext)</unmanaged>
        void DWrite.FontFileStream.ReadFileFragment(out IntPtr fragmentStart, long fileOffset, long fragmentSize, out IntPtr fragmentContext)
        {
            lock (this)
            {
                fragmentContext = IntPtr.Zero;

                _stream.Position = fileOffset;

                fragmentStart = _stream.PositionPointer;

            }
        }

        /// <summary>
        /// Releases a fragment from a file.
        /// </summary>
        /// <param name="fragmentContext">A reference to the client-defined context of a font fragment returned from {{ReadFileFragment}}.</param>
        /// <unmanaged>void IDWriteFontFileStream::ReleaseFileFragment([None] void* fragmentContext)</unmanaged>
        void DWrite.FontFileStream.ReleaseFileFragment(IntPtr fragmentContext)
        {
            // Nothing to release. No context are used
        }

        /// <summary>
        /// Obtains the total size of a file.
        /// </summary>
        /// <returns>the total size of the file.</returns>
        /// <remarks>
        /// Implementing GetFileSize() for asynchronously loaded font files may require downloading the complete file contents. Therefore, this method should be used only for operations that either require a complete font file to be loaded (for example, copying a font file) or that need to make decisions based on the value of the file size (for example, validation against a persisted file size).
        /// </remarks>
        /// <unmanaged>HRESULT IDWriteFontFileStream::GetFileSize([Out] __int64* fileSize)</unmanaged>
        long DWrite.FontFileStream.GetFileSize()
        {
            return _stream.Length;
        }

        /// <summary>
        /// Obtains the last modified time of the file.
        /// </summary>
        /// <returns>
        /// the last modified time of the file in the format that represents the number of 100-nanosecond intervals since January 1, 1601 (UTC).
        /// </returns>
        /// <remarks>
        /// The "last modified time" is used by DirectWrite font selection algorithms to determine whether one font resource is more up to date than another one.
        /// </remarks>
        /// <unmanaged>HRESULT IDWriteFontFileStream::GetLastWriteTime([Out] __int64* lastWriteTime)</unmanaged>
        long DWrite.FontFileStream.GetLastWriteTime()
        {
            return 0;
        }
    }

    /// <summary>
    /// Resource FontFileEnumerator.
    /// </summary>
    public class ResourceFontFileEnumerator : CallbackBase, DWrite.FontFileEnumerator
    {
        private DWrite.Factory _factory;
        private DWrite.FontFileLoader _loader;
        private DataStream keyStream;
        private DWrite.FontFile _currentFontFile;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceFontFileEnumerator"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="loader">The loader.</param>
        /// <param name="key">The key.</param>
        public ResourceFontFileEnumerator(DWrite.Factory factory, DWrite.FontFileLoader loader, DataPointer key)
        {
            _factory = factory;

            _loader = loader;

            keyStream = new DataStream(key.Pointer, key.Size, true, false);
        }

        /// <summary>
        /// Advances to the next font file in the collection. When it is first created, the enumerator is positioned before the first element of the collection and the first call to MoveNext advances to the first file.
        /// </summary>
        /// <returns>
        /// the value TRUE if the enumerator advances to a file; otherwise, FALSE if the enumerator advances past the last file in the collection.
        /// </returns>
        /// <unmanaged>HRESULT IDWriteFontFileEnumerator::MoveNext([Out] BOOL* hasCurrentFile)</unmanaged>
        bool DWrite.FontFileEnumerator.MoveNext()
        {
            bool moveNext = keyStream.RemainingLength != 0;

            if (!moveNext) return false;

            _currentFontFile?.Dispose();

            _currentFontFile = new DWrite.FontFile(_factory, keyStream.PositionPointer, 4, _loader);

            keyStream.Position += 4;

            return true;
        }

        /// <summary>
        /// Gets a reference to the current font file.
        /// </summary>
        /// <value></value>
        /// <returns>a reference to the newly created <see cref="SharpDX.DirectWrite.FontFile"/> object.</returns>
        /// <unmanaged>HRESULT IDWriteFontFileEnumerator::GetCurrentFontFile([Out] IDWriteFontFile** fontFile)</unmanaged>
        DWrite.FontFile DWrite.FontFileEnumerator.CurrentFontFile
        {
            get
            {
                ((IUnknown)_currentFontFile).AddReference();

                return _currentFontFile;
            }
        }
    }
}
