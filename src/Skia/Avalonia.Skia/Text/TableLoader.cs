// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using HarfBuzzSharp;
using SkiaSharp;

namespace Avalonia.Skia.Text
{
    internal class TableLoader : IDisposable
    {
        //ToDo: Introduce a proper cache that limits memory usage etc.
        private static readonly ConcurrentDictionary<SKTypeface, TableLoader> s_tableLoaderCache = new ConcurrentDictionary<SKTypeface, TableLoader>();

        private readonly SKTypeface _typeface;
        private readonly Face _face;
        private bool _isDisposed;

        public TableLoader(SKTypeface typeface)
        {
            _typeface = typeface;

            _face = new Face(GetTable, Dispose)
            {
                UnitsPerEm = _typeface.UnitsPerEm
            };

            Font = new Font(_face);

            Font.SetFunctionsOpenType();
        }

        public Font Font { get; }

        /// <summary>
        ///     Creates a new <see cref="TableLoader"/> on demand.
        /// </summary>
        /// <param name="typeface">The typeface.</param>
        /// <returns>The table loader.</returns>
        public static TableLoader Get(SKTypeface typeface)
        {
            return s_tableLoaderCache.GetOrAdd(typeface, CreateTableLoader);
        }

        private static TableLoader CreateTableLoader(SKTypeface typeface)
        {
            return new TableLoader(typeface);
        }

        private Blob GetTable(Face face, Tag tag) => CreateBlob(tag);

        private void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            if (!disposing)
            {
                return;
            }

            Font?.Dispose();
            _face?.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private Blob CreateBlob(Tag tag)
        {
            var size = _typeface.GetTableSize(tag);

            var data = Marshal.AllocCoTaskMem(size);

            var releaseDelegate = new ReleaseDelegate(() => Marshal.FreeCoTaskMem(data));

            return _typeface.TryGetTableData(tag, 0, size, data) ?
                new Blob(data, size, MemoryMode.Writeable, releaseDelegate) : null;
        }
    }
}
