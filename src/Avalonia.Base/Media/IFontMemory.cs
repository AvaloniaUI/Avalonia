using System;
using Avalonia.Media.Fonts;
using Avalonia.Metadata;

namespace Avalonia.Media
{
    [NotClientImplementable]
    public interface IFontMemory : IDisposable
    {
        /// <summary>
        /// Attempts to retrieve the memory block associated with the specified OpenType table tag.
        /// </summary>
        /// <param name="tag">The OpenType table tag identifying the table to retrieve.</param>
        /// <param name="table">When this method returns, contains the memory block of the specified table if the operation succeeds;
        /// otherwise, contains an empty memory block. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/> if the memory block for the specified table tag was successfully retrieved;
        /// otherwise, <see langword="false"/>.</returns>
        bool TryGetTable(OpenTypeTag tag, out ReadOnlyMemory<byte> table);
    }
}
