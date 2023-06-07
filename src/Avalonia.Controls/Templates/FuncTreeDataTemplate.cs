using System;
using System.Collections;
using Avalonia.Data;

namespace Avalonia.Controls.Templates
{
    /// <summary>
    /// A template used to build hierarchical data.
    /// </summary>
    public class FuncTreeDataTemplate : FuncDataTemplate, ITreeDataTemplate
    {
        private readonly Func<object?, IEnumerable> _itemsSelector;

        /// <summary>
        /// Initializes a new instance of the <see cref="FuncTreeDataTemplate"/> class.
        /// </summary>
        /// <param name="type">The type of data which the data template matches.</param>
        /// <param name="build">
        /// A function which when passed an object of <paramref name="type"/> returns a control.
        /// </param>
        /// <param name="itemsSelector">
        /// A function which when passed an object of <paramref name="type"/> returns the child
        /// items.
        /// </param>
        public FuncTreeDataTemplate(
            Type type,
            Func<object?, INameScope, Control> build,
            Func<object?, IEnumerable> itemsSelector)
            : this(o => IsInstance(o, type), build, itemsSelector)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FuncTreeDataTemplate"/> class.
        /// </summary>
        /// <param name="match">
        /// A function which determines whether the data template matches the specified data.
        /// </param>
        /// <param name="build">
        /// A function which when passed a matching object returns a control.
        /// </param>
        /// <param name="itemsSelector">
        /// A function which when passed a matching object returns the child items.
        /// </param>
        public FuncTreeDataTemplate(
            Func<object?, bool> match,
            Func<object?, INameScope, Control?> build,
            Func<object?, IEnumerable> itemsSelector)
            : base(match, build)
        {
            _itemsSelector = itemsSelector;
        }

        /// <summary>
        /// Selects the child items of an item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>The child items, or null if no child items.</returns>
        public InstancedBinding ItemsSelector(object item)
        {
            return InstancedBinding.OneTime(_itemsSelector(item));
        }

        /// <summary>
        /// Determines of an object is of the specified type.
        /// </summary>
        /// <param name="o">The object.</param>
        /// <param name="t">The type.</param>
        /// <returns>
        /// True if <paramref name="o"/> is of type <paramref name="t"/>, otherwise false.
        /// </returns>
        private static bool IsInstance(object? o, Type t)
        {
            return t.IsInstanceOfType(o);
        }
    }
}
