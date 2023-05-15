using Avalonia.Controls.Primitives;

namespace Avalonia.Controls.Templates
{
    /// <summary>
    /// Interface representing a template used to build a <see cref="TemplatedControl"/>.
    /// </summary>
    public interface IControlTemplate : ITemplate<TemplatedControl, TemplateResult<Control>?>
    {
    }
}
