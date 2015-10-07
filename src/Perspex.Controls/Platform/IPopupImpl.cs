// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Perspex.Platform
{
    /// <summary>
    /// Defines a platform-specific popup window implementation.
    /// </summary>
    public interface IPopupImpl : ITopLevelImpl
    {
        /// <summary>
        /// Sets the position of the popup.
        /// </summary>
        /// <param name="p">The position, in screen coordinates.</param>
        void SetPosition(Point p);

        /// <summary>
        /// Shows the popup.
        /// </summary>
        void Show();

        /// <summary>
        /// Hides the popup.
        /// </summary>
        void Hide();
    }
}
