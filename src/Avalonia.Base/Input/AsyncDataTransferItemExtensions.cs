namespace Avalonia.Input;

/// <summary>
/// Contains extension methods for <see cref="IAsyncDataTransferItem"/>.
/// </summary>
public static class AsyncDataTransferItemExtensions
{
    /// <summary>
    /// Gets whether a <see cref="IAsyncDataTransferItem"/> supports a specific format.
    /// </summary>
    /// <param name="dataTransferItem">The <see cref="IAsyncDataTransferItem"/> instance.</param>
    /// <param name="format">The format to check.</param>
    /// <returns>true if <paramref name="format"/> is supported, false otherwise.</returns>
    public static bool Contains(this IAsyncDataTransferItem dataTransferItem, DataFormat format)
    {
        var formats = dataTransferItem.Formats;
        var count = formats.Count;

        for (var i = 0; i < count; ++i)
        {
            if (format == formats[i])
                return true;
        }

        return false;
    }
}
