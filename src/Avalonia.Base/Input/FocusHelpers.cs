using System;
using System.Collections.Generic;
using Avalonia.VisualTree;

namespace Avalonia.Input
{
    internal static class FocusHelpers
    {
        /// <summary>
        /// Gets the visual children of a parent that implement IInputElement.
        /// Returns an allocation-free enumerable when the parent is a Visual.
        /// </summary>
        public static InputElementChildrenEnumerable GetInputElementChildren(AvaloniaObject? parent)
        {
            // TODO: add control overrides to return custom focus list from control
            if (parent is Visual visual)
            {
                return new InputElementChildrenEnumerable(visual.VisualChildren);
            }

            return default;
        }

        /// <summary>
        /// A struct enumerable that iterates visual children as IInputElement without allocation.
        /// </summary>
        internal readonly struct InputElementChildrenEnumerable
        {
            private readonly IReadOnlyList<Visual>? _children;

            public InputElementChildrenEnumerable(IReadOnlyList<Visual>? children)
            {
                _children = children;
            }

            public int Count => _children?.Count ?? 0;

            public IInputElement? this[int index]
            {
                get
                {
                    if (_children == null || index < 0 || index >= _children.Count)
                        return null;
                    return _children[index] as IInputElement;
                }
            }

            public Enumerator GetEnumerator() => new Enumerator(_children);

            public struct Enumerator
            {
                private readonly IReadOnlyList<Visual>? _children;
                private int _index;
                private IInputElement? _current;

                public Enumerator(IReadOnlyList<Visual>? children)
                {
                    _children = children;
                    _index = -1;
                    _current = null;
                }

                public IInputElement Current => _current!;

                public bool MoveNext()
                {
                    if (_children == null)
                        return false;

                    while (++_index < _children.Count)
                    {
                        if (_children[_index] is IInputElement input)
                        {
                            _current = input;
                            return true;
                        }
                    }

                    _current = null;
                    return false;
                }
            }
        }

        public static bool CanHaveFocusableChildren(AvaloniaObject? parent)
        {
            if (parent == null)
                return false;

            var children = GetInputElementChildren(parent);

            bool hasFocusChildren = true;

            foreach (var child in children)
            {
                if (IsVisible(child))
                {
                    if (child.Focusable)
                    {
                        hasFocusChildren = true;
                    }
                    else if (CanHaveFocusableChildren(child as AvaloniaObject))
                    {
                        hasFocusChildren = true;
                    }
                }

                if (hasFocusChildren)
                    break;
            }

            return hasFocusChildren;
        }

        public static IInputElement? GetFocusParent(IInputElement? inputElement)
        {
            if (inputElement == null)
                return null;

            if (inputElement is Visual visual)
            {
                var rootVisual = visual.VisualRoot;
                if (inputElement != rootVisual)
                    return visual.Parent as IInputElement;
            }

            return null;
        }

        public static bool IsPotentialTabStop(IInputElement? element)
        {
            if (element is InputElement inputElement)
                return inputElement.IsTabStop;

            return false;
        }

        internal static bool IsVisible(IInputElement? element)
        {
            if(element is Visual visual)
                return visual.IsEffectivelyVisible;

            return false;
        }

        internal static bool IsFocusable(IInputElement? element)
        {
            return element?.Focusable ?? false;
        }

        internal static bool CanHaveChildren(IInputElement? element)
        {
            // We don't currently have a flag to indicate a visual can have children, so we just return whether the element is a visual
            return element is Visual;
        }
    }
}
