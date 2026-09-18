using System;
using Avalonia.VisualTree;

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

    /// <summary>
    /// Sets a custom <see cref="CompositionVisual"/> as the last child of the element’s visual tree.
    /// </summary>
    public static void SetElementChildVisual(Visual visual, CompositionVisual? compositionVisual)
    {
        if (compositionVisual != null && visual.CompositionVisual != null &&
            compositionVisual.Compositor != visual.CompositionVisual.Compositor)
            throw new InvalidOperationException("Composition visuals belong to different compositor instances");

        // Un-parent the previous child visual eagerly. The renderer sync that
        // would normally reconcile the children may never run again for this
        // element — e.g. when the child visual is cleared from
        // OnDetachedFromVisualTree, which runs before DetachFromCompositor
        // discards the element's composition visual. Leaving the old child
        // parented there would make any later attach throw.
        if (visual.ChildCompositionVisual is { } oldChild && !ReferenceEquals(oldChild, compositionVisual))
            visual.CompositionVisual?.Children.Remove(oldChild);

        visual.ChildCompositionVisual = compositionVisual;
        visual.GetPresentationSource()?.Renderer.RecalculateChildren(visual);
    }

    /// <summary>
    /// Retrieves a <see cref="CompositionVisual"/> object previously set by a call to <see cref="SetElementChildVisual" />.
    /// </summary>
    public static CompositionVisual? GetElementChildVisual(Visual visual) => visual.ChildCompositionVisual;
}
