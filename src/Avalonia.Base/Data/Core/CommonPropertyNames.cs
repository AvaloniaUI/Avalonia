using System;

namespace Avalonia.Data.Core
{
    internal static class CommonPropertyNames
    {
        public const string IndexerName = "Item";

        // "Item[]" is what ObservableCollection<T> raises, "Item[key]" what AvaloniaDictionary raises.
        public static bool IsIndexerChange(string? propertyName) =>
            propertyName == IndexerName || propertyName?.StartsWith(IndexerName + "[", StringComparison.Ordinal) == true;
    }
}
