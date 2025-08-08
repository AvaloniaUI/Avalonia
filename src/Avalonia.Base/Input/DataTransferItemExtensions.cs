namespace Avalonia.Input;

/// <summary>
/// Contains extension methods for <see cref="IDataTransferItem"/>.
/// </summary>
public static class DataTransferItemExtensions
{
    /// <summary>
    /// Gets whether a <see cref="IDataTransferItem"/> supports a specific format.
    /// </summary>
    /// <param name="dataTransferItem">The <see cref="IDataTransferItem"/> instance.</param>
    /// <param name="format">The format to check.</param>
    /// <returns>true if <paramref name="format"/> is supported, false otherwise.</returns>
    public static bool Contains(this IDataTransferItem dataTransferItem, DataFormat format)
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
