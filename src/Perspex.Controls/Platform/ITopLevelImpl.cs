





namespace Perspex.Platform
{
    using System;
    using Perspex.Controls;
    using Perspex.Input;
    using Perspex.Input.Raw;

    public interface ITopLevelImpl : IDisposable
    {
        Size ClientSize { get; set; }

        IPlatformHandle Handle { get; }

        Action Activated { get; set; }

        Action Closed { get; set; }

        Action Deactivated { get; set; }

        Action<RawInputEventArgs> Input { get; set; }

        Action<Rect, IPlatformHandle> Paint { get; set; }

        Action<Size> Resized { get; set; }

        void Activate();

        void Invalidate(Rect rect);

        void SetOwner(TopLevel owner);

        Point PointToScreen(Point point);

        /// <summary>
        /// Sets the cursor associated with the window.
        /// </summary>
        /// <param name="cursor">The cursor. Use null for default cursor</param>
        void SetCursor(IPlatformHandle cursor);
    }
}
