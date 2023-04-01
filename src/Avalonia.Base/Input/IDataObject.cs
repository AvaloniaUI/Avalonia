using System.Collections.Generic;
using System.Linq;
using Avalonia.Platform.Storage;

namespace Avalonia.Input
{
    /// <summary>
    /// Interface to access information about the data of a drag-and-drop operation.
    /// </summary>
    public interface IDataObject
    {
        /// <summary>
        /// Lists all formats which are present in the DataObject.
        /// <seealso cref="DataFormats"/>
        /// </summary>
        IEnumerable<string> GetDataFormats();

        /// <summary>
        /// Checks whether a given DataFormat is present in this object
        /// <seealso cref="DataFormats"/>
        /// </summary>
        bool Contains(string dataFormat);

        /// <summary>
        /// Tries to get the data of the given DataFormat.
        /// </summary>
        /// <returns>
        /// Object data. If format isn't available, returns null.
        /// </returns>
        object? Get(string dataFormat);
    }
}
