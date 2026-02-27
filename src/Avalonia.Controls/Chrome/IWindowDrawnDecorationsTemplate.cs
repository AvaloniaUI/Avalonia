using Avalonia.Controls.Templates;
using Avalonia.Metadata;
using Avalonia.Styling;

namespace Avalonia.Controls.Chrome;

/// <summary>
/// Interface for a template that produces <see cref="WindowDrawnDecorationsContent"/>.
/// Implemented by the XAML template class in Avalonia.Markup.Xaml.
/// Extends <see cref="ITemplate"/> so the XAML compiler assigns the template object directly
/// instead of auto-calling Build().
/// </summary>
[ControlTemplateScope]
public interface IWindowDrawnDecorationsTemplate : ITemplate
{
    /// <summary>
    /// Builds the template and returns the content with its name scope.
    /// </summary>
    new TemplateResult<WindowDrawnDecorationsContent> Build();
}
