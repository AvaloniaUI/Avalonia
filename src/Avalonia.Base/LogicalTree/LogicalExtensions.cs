using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Avalonia.Collections;

namespace Avalonia.LogicalTree
{
    /// <summary>
    /// Provides extension methods for working with the logical tree.
    /// </summary>
    public static class LogicalExtensions
    {
        /// <summary>
        /// Enumerates the ancestors of an <see cref="ILogical"/> in the logical tree.
        /// </summary>
        /// <param name="logical">The logical.</param>
        /// <returns>The logical's ancestors.</returns>
        public static LogicalAncestorsEnumerable GetLogicalAncestors(this ILogical logical)
        {
            _ = logical ?? throw new ArgumentNullException(nameof(logical));
            return new LogicalAncestorsEnumerable(logical);
        }

        /// <summary>
        /// Enumerates an <see cref="ILogical"/> and its ancestors in the logical tree.
        /// </summary>
        /// <param name="logical">The logical.</param>
        /// <returns>The logical and its ancestors.</returns>
        public static SelfAndLogicalAncestorsEnumerable GetSelfAndLogicalAncestors(this ILogical logical)
        {
            return new SelfAndLogicalAncestorsEnumerable(logical);
        }

        /// <summary>
        /// Finds first ancestor of given type.
        /// </summary>
        /// <typeparam name="T">Ancestor type.</typeparam>
        /// <param name="logical">The logical.</param>
        /// <param name="includeSelf">If given logical should be included in search.</param>
        /// <returns>First ancestor of given type.</returns>
        public static T? FindLogicalAncestorOfType<T>(this ILogical? logical, bool includeSelf = false) where T : class
        {
            if (logical is null)
            {
                return null;
            }

            var parent = includeSelf ? logical : logical.LogicalParent;

            while (parent != null)
            {
                if (parent is T result)
                {
                    return result;
                }

                parent = parent.LogicalParent;
            }

            return null;
        }

        /// <summary>
        /// Enumerates the children of an <see cref="ILogical"/> in the logical tree.
        /// </summary>
        /// <param name="logical">The logical.</param>
        /// <returns>The logical children.</returns>
        public static IEnumerable<ILogical> GetLogicalChildren(this ILogical logical)
        {
            return logical.LogicalChildren;
        }

        /// <summary>
        /// Enumerates the descendants of an <see cref="ILogical"/> in the logical tree.
        /// </summary>
        /// <param name="logical">The logical.</param>
        /// <returns>The logical's ancestors.</returns>
        public static LogicalDescendantsEnumerable GetLogicalDescendants(this ILogical logical)
        {
            return new LogicalDescendantsEnumerable(logical);
        }

        /// <summary>
        /// Enumerates an <see cref="ILogical"/> and its descendants in the logical tree.
        /// </summary>
        /// <param name="logical">The logical.</param>
        /// <returns>The logical and its ancestors.</returns>
        public static SelfAndLogicalDescendantsEnumerable GetSelfAndLogicalDescendants(this ILogical logical)
        {
            return new SelfAndLogicalDescendantsEnumerable(logical);
        }

        /// <summary>
        /// Finds first descendant of given type.
        /// </summary>
        /// <typeparam name="T">Descendant type.</typeparam>
        /// <param name="logical">The logical.</param>
        /// <param name="includeSelf">If given logical should be included in search.</param>
        /// <returns>First descendant of given type.</returns>
        public static T? FindLogicalDescendantOfType<T>(this ILogical? logical, bool includeSelf = false) where T : class
        {
            if (logical is null)
            {
                return null;
            }

            if (includeSelf && logical is T result)
            {
                return result;
            }

            return FindDescendantOfTypeCore<T>(logical);
        }

        /// <summary>
        /// Gets the logical parent of an <see cref="ILogical"/>.
        /// </summary>
        /// <param name="logical">The logical.</param>
        /// <returns>The parent, or null if the logical is unparented.</returns>
        public static ILogical? GetLogicalParent(this ILogical logical)
        {
            return logical.LogicalParent;
        }

        /// <summary>
        /// Gets the logical parent of an <see cref="ILogical"/>.
        /// </summary>
        /// <typeparam name="T">The type of the logical parent.</typeparam>
        /// <param name="logical">The logical.</param>
        /// <returns>
        /// The parent, or null if the logical is unparented or its parent is not of type <typeparamref name="T"/>.
        /// </returns>
        public static T? GetLogicalParent<T>(this ILogical logical) where T : class
        {
            return logical.LogicalParent as T;
        }

        /// <summary>
        /// Enumerates the siblings of an <see cref="ILogical"/> in the logical tree.
        /// </summary>
        /// <param name="logical">The logical.</param>
        /// <returns>The logical siblings.</returns>
        public static IEnumerable<ILogical> GetLogicalSiblings(this ILogical logical)
        {
            var parent = logical.LogicalParent;

            if (parent != null)
            {
                foreach (ILogical sibling in parent.LogicalChildren)
                {
                    yield return sibling;
                }
            }
        }

        /// <summary>
        /// Tests whether an <see cref="ILogical"/> is an ancestor of another logical.
        /// </summary>
        /// <param name="logical">The logical.</param>
        /// <param name="target">The potential descendant.</param>
        /// <returns>
        /// True if <paramref name="logical"/> is an ancestor of <paramref name="target"/>;
        /// otherwise false.
        /// </returns>
        public static bool IsLogicalAncestorOf(this ILogical? logical, ILogical? target)
        {
            var current = target?.LogicalParent;

            while (current != null)
            {
                if (current == logical)
                {
                    return true;
                }

                current = current.LogicalParent;
            }

            return false;
        }

        private static T? FindDescendantOfTypeCore<T>(ILogical logical) where T : class
        {
            var logicalChildren = logical.LogicalChildren;
            var logicalChildrenCount = logicalChildren.Count;

            for (var i = 0; i < logicalChildrenCount; i++)
            {
                ILogical child = logicalChildren[i];

                if (child is T result)
                {
                    return result;
                }

                var childResult = FindDescendantOfTypeCore<T>(child);

                if (!(childResult is null))
                {
                    return childResult;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// A struct-based enumerable for logical descendants to avoid allocations.
    /// </summary>
    public readonly struct LogicalDescendantsEnumerable : IEnumerable<ILogical>
    {
        private readonly ILogical? _root;

        internal LogicalDescendantsEnumerable(ILogical? root)
        {
            _root = root;
        }

        /// <summary>
        /// Gets the struct enumerator (avoids allocation when used in foreach).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LogicalDescendantsEnumerator GetEnumerator() => new(_root);

        IEnumerator<ILogical> IEnumerable<ILogical>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// A struct-based enumerator for logical descendants using depth-first traversal.
    /// </summary>
    public struct LogicalDescendantsEnumerator : IEnumerator<ILogical>
    {
        private readonly Stack<(ILogical parent, int index)>? _stack;
        private ILogical? _current;
        private ILogical? _currentParent;
        private int _currentIndex;

        internal LogicalDescendantsEnumerator(ILogical? root)
        {
            _current = null;
            _currentParent = root;
            _currentIndex = 0;

            if (root != null && root.LogicalChildren.Count > 0)
            {
                _stack = new Stack<(ILogical, int)>(16);
            }
            else
            {
                _stack = null;
            }
        }

        /// <summary>
        /// Gets the current logical.
        /// </summary>
        public ILogical Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current!;
        }

        object IEnumerator.Current => Current;

        /// <summary>
        /// Moves to the next descendant using depth-first traversal.
        /// </summary>
        public bool MoveNext()
        {
            if (_currentParent == null)
                return false;

            IAvaloniaReadOnlyList<ILogical> children = _currentParent.LogicalChildren;

            // Try to get next child at current level
            while (_currentIndex < children.Count)
            {
                var child = children[_currentIndex];
                _currentIndex++;
                _current = child;

                // If this child has children, push current state and descend
                if (child.LogicalChildren.Count > 0)
                {
                    _stack?.Push((_currentParent, _currentIndex));
                    _currentParent = child;
                    _currentIndex = 0;
                }

                return true;
            }

            // Pop back up the stack
            while (_stack != null && _stack.Count > 0)
            {
                (_currentParent, _currentIndex) = _stack.Pop();
                children = _currentParent.LogicalChildren;

                while (_currentIndex < children.Count)
                {
                    var child = children[_currentIndex];
                    _currentIndex++;
                    _current = child;

                    if (child.LogicalChildren.Count > 0)
                    {
                        _stack.Push((_currentParent, _currentIndex));
                        _currentParent = child;
                        _currentIndex = 0;
                    }

                    return true;
                }
            }

            _currentParent = null;
            return false;
        }

        /// <summary>
        /// Resets the enumerator.
        /// </summary>
        public void Reset()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Disposes the enumerator.
        /// </summary>
        public void Dispose()
        {
            // Nothing to dispose
        }
    }

    /// <summary>
    /// A struct-based enumerable for logical ancestors to avoid allocations.
    /// </summary>
    public readonly struct LogicalAncestorsEnumerable : IEnumerable<ILogical>
    {
        private readonly ILogical? _logical;

        internal LogicalAncestorsEnumerable(ILogical? logical)
        {
            _logical = logical;
        }

        /// <summary>
        /// Gets the struct enumerator (avoids allocation when used in foreach).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LogicalAncestorsEnumerator GetEnumerator() => new(_logical);

        IEnumerator<ILogical> IEnumerable<ILogical>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// A struct-based enumerator for logical ancestors.
    /// </summary>
    public struct LogicalAncestorsEnumerator : IEnumerator<ILogical>
    {
        private ILogical? _current;

        internal LogicalAncestorsEnumerator(ILogical? logical)
        {
            _current = logical;
        }

        /// <summary>
        /// Gets the current logical.
        /// </summary>
        public ILogical Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current!;
        }

        object IEnumerator.Current => Current;

        /// <summary>
        /// Moves to the next ancestor.
        /// </summary>
        public bool MoveNext()
        {
            _current = _current?.LogicalParent;
            return _current != null;
        }

        /// <summary>
        /// Resets the enumerator.
        /// </summary>
        public void Reset()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Disposes the enumerator.
        /// </summary>
        public void Dispose()
        {
            // Nothing to dispose
        }
    }

    /// <summary>
    /// A struct-based enumerable for self and logical ancestors to avoid allocations.
    /// </summary>
    public readonly struct SelfAndLogicalAncestorsEnumerable : IEnumerable<ILogical>
    {
        private readonly ILogical? _logical;

        internal SelfAndLogicalAncestorsEnumerable(ILogical? logical)
        {
            _logical = logical;
        }

        /// <summary>
        /// Gets the struct enumerator (avoids allocation when used in foreach).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SelfAndLogicalAncestorsEnumerator GetEnumerator() => new(_logical);

        IEnumerator<ILogical> IEnumerable<ILogical>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// A struct-based enumerator for self and logical ancestors.
    /// </summary>
    public struct SelfAndLogicalAncestorsEnumerator : IEnumerator<ILogical>
    {
        private ILogical? _current;
        private bool _includeSelf;

        internal SelfAndLogicalAncestorsEnumerator(ILogical? logical)
        {
            _current = logical;
            _includeSelf = true;
        }

        /// <summary>
        /// Gets the current logical.
        /// </summary>
        public ILogical Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current!;
        }

        object IEnumerator.Current => Current;

        /// <summary>
        /// Moves to the next element (self first, then ancestors).
        /// </summary>
        public bool MoveNext()
        {
            if (_includeSelf)
            {
                _includeSelf = false;
                return _current != null;
            }

            _current = _current?.LogicalParent;
            return _current != null;
        }

        /// <summary>
        /// Resets the enumerator.
        /// </summary>
        public void Reset()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Disposes the enumerator.
        /// </summary>
        public void Dispose()
        {
            // Nothing to dispose
        }
    }

    /// <summary>
    /// A struct-based enumerable for self and logical descendants to avoid allocations.
    /// </summary>
    public readonly struct SelfAndLogicalDescendantsEnumerable : IEnumerable<ILogical>
    {
        private readonly ILogical? _root;

        internal SelfAndLogicalDescendantsEnumerable(ILogical? root)
        {
            _root = root;
        }

        /// <summary>
        /// Gets the struct enumerator (avoids allocation when used in foreach).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SelfAndLogicalDescendantsEnumerator GetEnumerator() => new(_root);

        IEnumerator<ILogical> IEnumerable<ILogical>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// A struct-based enumerator for self and logical descendants using depth-first traversal.
    /// </summary>
    public struct SelfAndLogicalDescendantsEnumerator : IEnumerator<ILogical>
    {
        private LogicalDescendantsEnumerator _descendantsEnumerator;
        private ILogical? _root;
        private bool _includeSelf;

        internal SelfAndLogicalDescendantsEnumerator(ILogical? root)
        {
            _root = root;
            _includeSelf = true;
            _descendantsEnumerator = new LogicalDescendantsEnumerator(root);
        }

        /// <summary>
        /// Gets the current logical.
        /// </summary>
        public ILogical Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _includeSelf ? _root! : _descendantsEnumerator.Current;
        }

        object IEnumerator.Current => Current;

        /// <summary>
        /// Moves to the next element (self first, then descendants).
        /// </summary>
        public bool MoveNext()
        {
            if (_includeSelf)
            {
                _includeSelf = false;
                return _root != null;
            }

            return _descendantsEnumerator.MoveNext();
        }

        /// <summary>
        /// Resets the enumerator.
        /// </summary>
        public void Reset()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Disposes the enumerator.
        /// </summary>
        public void Dispose()
        {
            _descendantsEnumerator.Dispose();
        }
    }
}
