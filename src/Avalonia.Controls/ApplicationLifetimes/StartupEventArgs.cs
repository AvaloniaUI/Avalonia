// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Avalonia.Controls.ApplicationLifetimes
{
    /// <summary>
    /// Contains the arguments for the <see cref="IControlledApplicationLifetime.Startup"/> event.
    /// </summary>
    public class ControlledApplicationLifetimeStartupEventArgs : EventArgs
    {
        public ControlledApplicationLifetimeStartupEventArgs(IEnumerable<string> args)
        {
            Args = args?.ToArray() ?? Array.Empty<string>();
        }

        public string[] Args { get;  }
    }
}
