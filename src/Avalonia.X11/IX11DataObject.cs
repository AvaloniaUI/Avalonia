using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Input;

namespace Avalonia.X11
{
    /// <summary>
    ///  Interface to access information about the data of a drag-and-drop operation on X11 window system.
    ///  Allows user to select X11's mime data type to recieve.
    /// </summary>
    /// <remarks>
    /// X11 communication protocol require us to select data type in moment of Drop event, but Avalonia's 
    /// <seealso cref="IDataObject"/> allows user to request data in preferred type in any time after.
    /// So we will select <seealso cref="DataFormats.Files"/>, <seealso cref="DataFormats.Text"/> or first of
    /// object's supported types, if user did not select preferred type during <seealso cref="DragDrop.DragEnterEvent"/>
    /// or <seealso cref="DragDrop.DragOverEvent"/>
    /// </remarks>
    /// <example>
    /// bool isSupportBmp = _x11DataObject.ReserveType("image/bmp");
    /// </example>
    public interface IX11DataObject : IDataObject, IDisposable
    {
        /// <summary>
        /// Allow to select preferred data type during <seealso cref="DragDrop.DragEnterEvent"/>
        /// or <seealso cref="DragDrop.DragOverEvent"/>.
        /// </summary>
        /// <param name="typeName">Mime data type.</param>
        /// <returns>True if data object supports this data and another type was not requested.</returns>
        public bool ReserveType(string typeName);
    }
}
