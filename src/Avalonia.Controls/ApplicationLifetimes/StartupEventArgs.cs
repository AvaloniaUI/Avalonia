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
