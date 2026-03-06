using Avalonia.Controls.Primitives;
using Avalonia.Metadata;

namespace Avalonia.Controls.Templates
{
    /// <summary>
    /// Interface representing a template used to build a <see cref="TemplatedControl"/>.
    /// </summary>
    [ControlTemplateScope]
    public interface IControlTemplate : ITemplate<TemplatedControl, TemplateResult<Control>?>
    {
    }
}
