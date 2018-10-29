// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace Avalonia.Controls
{
    /// <summary>
    /// Interface for Avalonia controls.
    /// </summary>
    public interface IControl : IVisual,
        IDataTemplateHost,
        ILayoutable,
        IInputElement,
        INamed,
        IStyledElement
    {
        new IControl Parent { get; }
    }
}
