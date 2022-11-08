using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Controls
{
    public interface IBiDirectionalNavigationRouter : INavigationRouter
    {
        /// <summary>
        /// Navigates to the next page in the stack if there is one.
        /// </summary>
        /// <returns>Task to await the navigation process.</returns>
        Task ForwardAsync();

        /// <summary>
        /// True if its possible to navigate forward.
        /// </summary>
        bool CanGoForward { get; }
    }
}
