// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Styling;

namespace Avalonia.Controls
{
    /// <summary>
    /// Creates a control.
    /// </summary>
    /// <typeparam name="TControl">The type of control.</typeparam>
    public interface ITemplate<TControl> : ITemplate where TControl : IControl
    {
        /// <summary>
        /// Creates the control.
        /// </summary>
        /// <returns>
        /// The created control.
        /// </returns>
        new TControl Build();
    }
}