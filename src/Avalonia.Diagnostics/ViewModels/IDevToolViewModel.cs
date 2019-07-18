// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.Diagnostics.ViewModels
{
    /// <summary>
    /// View model interface for tool showing up in DevTools
    /// </summary>
    public interface IDevToolViewModel
    {
        /// <summary>
        /// Name of a tool.
        /// </summary>
        string Name { get; }
    }
}
