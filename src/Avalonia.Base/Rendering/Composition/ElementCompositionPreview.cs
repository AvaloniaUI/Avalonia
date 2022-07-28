// Special license applies, see //file: src/Avalonia.Base/Rendering/Composition/License.md

namespace Avalonia.Rendering.Composition;

/// <summary>
/// Enables access to composition visual objects that back XAML elements in the XAML composition tree.
/// </summary>
public static class ElementComposition
{
    /// <summary>
    /// Gets CompositionVisual that backs a Visual
    /// </summary>
    /// <param name="visual"></param>
    /// <returns></returns>
    public static CompositionVisual? GetElementVisual(Visual visual) => visual.CompositionVisual;
}