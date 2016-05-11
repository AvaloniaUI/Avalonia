// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

namespace Avalonia.Interactivity
{
    /// <summary>
    /// Provides extension methods for the <see cref="IInteractive"/> interface.
    /// </summary>
    public static class InteractiveExtensions
    {
        /// <summary>
        /// Gets the route for bubbling events from the specified interactive.
        /// </summary>
        /// <param name="interactive">The interactive.</param>
        /// <returns>The event route.</returns>
        public static IEnumerable<IInteractive> GetBubbleEventRoute(this IInteractive interactive)
        {
            while (interactive != null)
            {
                yield return interactive;
                interactive = interactive.InteractiveParent;
            }
        }

        /// <summary>
        /// Gets the route for tunneling events from the specified interactive.
        /// </summary>
        /// <param name="interactive">The interactive.</param>
        /// <returns>The event route.</returns>
        public static IEnumerable<IInteractive> GetTunnelEventRoute(this IInteractive interactive)
        {
            return interactive.GetBubbleEventRoute().Reverse();
        }
    }
}
