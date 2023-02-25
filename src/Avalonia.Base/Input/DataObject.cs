using System.Collections.Generic;

namespace Avalonia.Input
{
    /// <summary>
    /// Specific and mutable implementation of the IDataObject interface.
    /// </summary>
    public class DataObject : IDataObject
    {
        private readonly Dictionary<string, object> _items = new();

        /// <inheritdoc />
        public bool Contains(string dataFormat)
        {
            return _items.ContainsKey(dataFormat);
        }

        /// <inheritdoc />
        public object? Get(string dataFormat)
        {
            return _items.TryGetValue(dataFormat, out var item) ? item : null;
        }

        /// <inheritdoc />
        public IEnumerable<string> GetDataFormats()
        {
            return _items.Keys;
        }

        /// <summary>
        /// Sets a value to the internal store of the data object with <see cref="DataFormats"/> as a key.
        /// </summary>
        public void Set(string dataFormat, object value)
        {
            _items[dataFormat] = value;
        }
    }
}
