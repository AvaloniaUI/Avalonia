using Avalonia.Rendering.Composition.Drawing;

namespace Avalonia.Rendering.Composition;

internal interface ICompositionVisualWithDrawList
{
    CompositionDrawList? DrawList { get; }
}