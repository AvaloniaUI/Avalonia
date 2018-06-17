// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia
{
    /// <summary>
    /// Enum for ExitMode
    /// </summary>
    public enum ExitMode
    {
        /// <summary>
        /// Indicates an implicit call to Application.Exit when the last window closes.
        /// </summary>
        OnLastWindowClose,

        /// <summary>
        /// Indicates an implicit call to Application.Exit when the main window closes.
        /// </summary>
        OnMainWindowClose,

        /// <summary>
        /// Indicates that the application only exits on an explicit call to Application.Exit.
        /// </summary>
        OnExplicitExit
    }
}