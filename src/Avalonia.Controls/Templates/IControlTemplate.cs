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
}
