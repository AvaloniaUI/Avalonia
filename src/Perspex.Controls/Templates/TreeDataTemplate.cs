





namespace Perspex.Controls.Templates
{
    using System;
    using System.Collections;
    using System.Reflection;

    /// <summary>
    /// A template used to build hierachical data.
    /// </summary>
    public class TreeDataTemplate : DataTemplate, ITreeDataTemplate
    {
        private Func<object, IEnumerable> itemsSelector;

        private Func<object, bool> isExpanded;

        /// <summary>
        /// Initializes a new instance of the <see cref="TreeDataTemplate"/> class.
        /// </summary>
        /// <param name="type">The type of data which the data template matches.</param>
        /// <param name="build">
        /// A function which when passed an object of <paramref name="type"/> returns a control.
        /// </param>
        /// <param name="itemsSelector">
        /// A function which when passed an object of <paramref name="type"/> returns the child
        /// items.
        /// </param>
        public TreeDataTemplate(
            Type type,
            Func<object, IControl> build,
            Func<object, IEnumerable> itemsSelector)
            : this(o => IsInstance(o, type), build, itemsSelector)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TreeDataTemplate"/> class.
        /// </summary>
        /// <param name="type">The type of data which the data template matches.</param>
        /// <param name="build">
        /// A function which when passed an object of <paramref name="type"/> returns a control.
        /// </param>
        /// <param name="itemsSelector">
        /// A function which when passed an object of <paramref name="type"/> returns the child
        /// items.
        /// </param>
        /// <param name="isExpanded">
        /// A function which when passed an object of <paramref name="type"/> returns the the
        /// initial expanded state of the node.
        /// </param>
        public TreeDataTemplate(
            Type type,
            Func<object, IControl> build,
            Func<object, IEnumerable> itemsSelector,
            Func<object, bool> isExpanded)
            : this(o => IsInstance(o, type), build, itemsSelector, isExpanded)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TreeDataTemplate"/> class.
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
        public TreeDataTemplate(
            Func<object, bool> match,
            Func<object, IControl> build,
            Func<object, IEnumerable> itemsSelector)
            : this(match, build, itemsSelector, _ => false)
        {
            this.itemsSelector = itemsSelector;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TreeDataTemplate"/> class.
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
        /// <param name="isExpanded">
        /// A function which when passed a matching object returns the the initial expanded state
        /// of the node.
        /// </param>
        public TreeDataTemplate(
            Func<object, bool> match,
            Func<object, IControl> build,
            Func<object, IEnumerable> itemsSelector,
            Func<object, bool> isExpanded)
            : base(match, build)
        {
            this.itemsSelector = itemsSelector;
            this.isExpanded = isExpanded;
        }

        /// <summary>
        /// Checks to see if the item should be initially expanded.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>True if the item should be initially expanded, otherwise false.</returns>
        public bool IsExpanded(object item)
        {
            return this?.isExpanded(item) ?? false;
        }

        /// <summary>
        /// Selects the child items of an item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>The child items, or null if no child items.</returns>
        public IEnumerable ItemsSelector(object item)
        {
            return this?.itemsSelector(item);
        }

        /// <summary>
        /// Determines of an object is of the specified type.
        /// </summary>
        /// <param name="o">The object.</param>
        /// <param name="t">The type.</param>
        /// <returns>
        /// True if <paramref name="o"/> is of type <paramref name="t"/>, otherwise false.
        /// </returns>
        private static bool IsInstance(object o, Type t)
        {
            return (o != null) ?
                t.GetTypeInfo().IsAssignableFrom(o.GetType().GetTypeInfo()) :
                false;
        }
    }
}
