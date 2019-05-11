// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Controls
{
    /// <summary>
    /// Contains the arguments for the <see cref="IApplicationLifecycle.Exit"/> event.
    /// </summary>
    public class ExitEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the exit code that an application returns to the operating system when the application exits.
        /// </summary>
        public int ApplicationExitCode { get; set; }
    }
}
