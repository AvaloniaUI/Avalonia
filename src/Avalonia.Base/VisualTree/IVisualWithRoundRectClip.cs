using System;
using System.ComponentModel;

namespace Avalonia.VisualTree
{
    [Obsolete("Internal API, will be removed in future versions, you've been warned"), EditorBrowsable(EditorBrowsableState.Never)]
    public interface IVisualWithRoundRectClip
    {
        /// <summary>
        /// Gets a value indicating the corner radius of control's clip bounds
        /// </summary>
        [Obsolete("Internal API, will be removed in future versions, you've been warned"), EditorBrowsable(EditorBrowsableState.Never)]
        CornerRadius ClipToBoundsRadius { get; }

    }
}
