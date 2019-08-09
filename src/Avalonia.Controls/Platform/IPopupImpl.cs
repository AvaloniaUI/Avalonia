// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Controls.Primitives.PopupPositioning;

namespace Avalonia.Platform
{
    /// <summary>
    /// Defines a platform-specific popup window implementation.
    /// </summary>
    public interface IPopupImpl : IWindowBaseImpl
    {
        IPopupPositioner PopupPositioner { get; }
    }
}
