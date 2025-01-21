using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// Specifies a contract for a scrolling control that supports multiple scroll snap points sources.
    /// </summary>
    internal interface IScrollSnapPointsSourceHost
    {
        /// <summary>
        /// Registers a control as a scroll snap points source.
        /// </summary>
        /// <param name="scrollSnapPointsInfo">
        /// A control within the subtree of the <see cref="IScrollSnapPointsInfo"/>.
        /// </param>
        void RegisterScrollSnapPointsInfoSource(IScrollSnapPointsInfo scrollSnapPointsInfo);

        /// <summary>
        /// Unregisters a control as a scroll snap points source.
        /// </summary>
        /// <param name="scrollSnapPointsInfo">
        /// A control within the subtree of the <see cref="IScrollSnapPointsInfo"/>.
        /// </param>
        void UnregisterScrollSnapPointsInfoSource(IScrollSnapPointsInfo scrollSnapPointsInfo);
    }
}
