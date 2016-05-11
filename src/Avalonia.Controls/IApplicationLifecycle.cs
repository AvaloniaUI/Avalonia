using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Controls
{
    /// <summary>
    /// Sends events about the application lifecycle.
    /// </summary>
    public interface IApplicationLifecycle
    {
        /// <summary>
        /// Sent when the application is exiting.
        /// </summary>
        event EventHandler OnExit;

        /// <summary>
        /// Exits the application.
        /// </summary>
        void Exit();
    }
}
