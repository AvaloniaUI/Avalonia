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

    public class ControlTemplateResult : TemplateResult<Control>
    {
        public Control Control { get; }

        public ControlTemplateResult(Control control, INameScope nameScope) : base(control, nameScope)
        {
            Control = control;
        }

        public new void Deconstruct(out Control control, out INameScope scope)
        {
            control = Control;
            scope = NameScope;
        }
    }
}
