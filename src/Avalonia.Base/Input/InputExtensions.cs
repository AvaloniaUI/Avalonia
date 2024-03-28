using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.VisualTree;

#nullable enable

namespace Avalonia.Input
{
    /// <summary>
    /// Defines extensions for the <see cref="IInputElement"/> interface.
    /// </summary>
    public static class InputExtensions
    {
        private static readonly Func<Visual, bool> s_hitTestDelegate = IsHitTestVisible;
        private static readonly Func<Visual, bool> s_hitTestEnabledOnlyDelegate = IsHitTestVisible_EnabledOnly;

        /// <summary>
        /// Returns the active input elements at a point on an <see cref="IInputElement"/>.
        /// </summary>
        /// <param name="element">The element to test.</param>
        /// <param name="p">The point on <paramref name="element"/>.</param>
        /// <param name="enabledElementsOnly">Whether to only return elements for which <see cref="IInputElement.IsEffectivelyEnabled"/> is true.</param>
        /// <returns>
        /// The active input elements found at the point, ordered topmost first.
        /// </returns>
        public static IEnumerable<IInputElement> GetInputElementsAt(this IInputElement element, Point p, bool enabledElementsOnly = true)
        {
            element = element ?? throw new ArgumentNullException(nameof(element));

            return (element as Visual)?.GetVisualsAt(p, enabledElementsOnly ? s_hitTestEnabledOnlyDelegate : s_hitTestDelegate).Cast<IInputElement>() ??
                Enumerable.Empty<IInputElement>();
        }
        
        /// <inheritdoc cref="GetInputElementsAt(IInputElement, Point, bool)"/>
        public static IEnumerable<IInputElement> GetInputElementsAt(this IInputElement element, Point p) => GetInputElementsAt(element, p, true);

        /// <summary>
        /// Returns the topmost active input element at a point on an <see cref="IInputElement"/>.
        /// </summary>
        /// <param name="element">The element to test.</param>
        /// <param name="p">The point on <paramref name="element"/>.</param>
        /// <param name="enabledElementsOnly">Whether to only return elements for which <see cref="IInputElement.IsEffectivelyEnabled"/> is true.</param>
        /// <returns>The topmost <see cref="IInputElement"/> at the specified position.</returns>
        public static IInputElement? InputHitTest(this IInputElement element, Point p, bool enabledElementsOnly = true)
        {
            element = element ?? throw new ArgumentNullException(nameof(element));

            return (element as Visual)?.GetVisualAt(p, enabledElementsOnly ? s_hitTestEnabledOnlyDelegate : s_hitTestDelegate) as IInputElement;
        }

        /// <inheritdoc cref="InputHitTest(IInputElement, Point, bool)"/>
        public static IInputElement? InputHitTest(this IInputElement element, Point p) => InputHitTest(element, p, true);

        /// <summary>
        /// Returns the topmost active input element at a point on an <see cref="IInputElement"/>.
        /// </summary>
        /// <param name="element">The element to test.</param>
        /// <param name="p">The point on <paramref name="element"/>.</param>
        /// <param name="filter">
        /// A filter predicate. If the predicate returns false then the visual and all its
        /// children will be excluded from the results.
        /// </param>
        /// <param name="enabledElementsOnly">Whether to only return elements for which <see cref="IInputElement.IsEffectivelyEnabled"/> is true.</param>
        /// <returns>The topmost <see cref="IInputElement"/> at the specified position.</returns>
        public static IInputElement? InputHitTest(
            this IInputElement element,
            Point p,
            Func<Visual, bool> filter,
            bool enabledElementsOnly = true)
        {
            element = element ?? throw new ArgumentNullException(nameof(element));
            filter = filter ?? throw new ArgumentNullException(nameof(filter));
            var hitTestDelegate = enabledElementsOnly ? s_hitTestEnabledOnlyDelegate : s_hitTestDelegate;

            return (element as Visual)?.GetVisualAt(p, x => hitTestDelegate(x) && filter(x)) as IInputElement;
        }

        /// <inheritdoc cref="InputHitTest(IInputElement, Point, Func{Visual, bool}, bool)"/>
        public static IInputElement? InputHitTest(this IInputElement element, Point p, Func<Visual, bool> filter) => InputHitTest(element, p, filter, true);

        private static bool IsHitTestVisible(Visual visual) => visual is { IsVisible: true, IsAttachedToVisualTree: true } and IInputElement { IsHitTestVisible: true };

        private static bool IsHitTestVisible_EnabledOnly(Visual visual) => IsHitTestVisible(visual) && visual is IInputElement { IsEffectivelyEnabled: true };
    }
}
