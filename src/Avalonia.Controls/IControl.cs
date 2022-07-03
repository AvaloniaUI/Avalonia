using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Metadata;
using Avalonia.VisualTree;

namespace Avalonia.Controls
{
    /// <summary>
    /// Interface for Avalonia controls.
    /// </summary>
    [NotClientImplementable]
    public interface IControl : IVisual,
        IDataTemplateHost,
        ILayoutable,
        IInputElement,
        INamed,
        IStyledElement
    {
        new IControl? Parent { get; }
    }
}
