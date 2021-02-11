using Avalonia.Controls.Primitives;
using Avalonia.Styling;

namespace Avalonia.Controls.Templates
{
    /// <summary>
    /// Interface representing a template used to build a <see cref="TemplatedControl"/>.
    /// </summary>
    public interface IControlTemplate : ITemplate<ITemplatedControl, TemplateResult<IControl>>
    {
    }

    public class TemplateResult<T>
    {
        public T Control { get; }
        public INameScope NameScope { get; }

        public TemplateResult(T control, INameScope nameScope)
        {
            Control = control;
            NameScope = nameScope;
        }

        public void Deconstruct(out T control, out INameScope scope)
        {
            control = Control;
            scope = NameScope;
        }
    }
}
