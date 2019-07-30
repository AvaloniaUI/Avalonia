// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Controls.Primitives;
using Avalonia.Styling;

namespace Avalonia.Controls.Templates
{
    /// <summary>
    /// Interface representing a template used to build a <see cref="TemplatedControl"/>.
    /// </summary>
    public interface IControlTemplate : ITemplate<ITemplatedControl, ControlTemplateResult>
    {
    }

    public class ControlTemplateResult
    {
        public IControl Control { get; }
        public INameScope NameScope { get; }

        public ControlTemplateResult(IControl control, INameScope nameScope)
        {
            Control = control;
            NameScope = nameScope;
        }

        public void Deconstruct(out IControl control, out INameScope scope)
        {
            control = Control;
            scope = NameScope;
        }
    }
}
