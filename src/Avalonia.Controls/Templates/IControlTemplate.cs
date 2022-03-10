using System;
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

    public class ControlTemplateResult : TemplateResult<IControl>
    {
        public IControl Control { get; }

        public ControlTemplateResult(IControl control, INameScope nameScope) : base(control, nameScope)
        {
            Control = control;
        }

        public new void Deconstruct(out IControl control, out INameScope scope)
        {
            control = Control;
            scope = NameScope;
        }
    }
}
