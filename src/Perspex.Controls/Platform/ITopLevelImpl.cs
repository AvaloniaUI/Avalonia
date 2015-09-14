// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Controls;
using Perspex.Input;
using Perspex.Input.Raw;

namespace Perspex.Platform
{
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
