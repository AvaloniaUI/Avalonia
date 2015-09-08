





namespace Perspex.Controls.Templates
{
    using System;
    using System.Collections;

    /// <summary>
    /// A template used to build hierachical data.
    /// </summary>
    /// <typeparam name="T">The type of the template's data.</typeparam>
    public class TreeDataTemplate<T> : TreeDataTemplate
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TreeDataTemplate{T}"/> class.
        /// </summary>
        /// <param name="build">
        /// A function which when passed an object of <typeparamref name="T"/> returns a control.
        /// </param>
        /// <param name="itemsSelector">
        /// A function which when passed an object of <typeparamref name="T"/> returns the child
        /// items.
        /// </param>
        public TreeDataTemplate(
            Func<T, Control> build,
            Func<T, IEnumerable> itemsSelector)
            : base(
                typeof(T),
                TreeDataTemplate<T>.Cast(build),
                TreeDataTemplate<T>.Cast(itemsSelector))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TreeDataTemplate{T}"/> class.
        /// </summary>
        /// <param name="build">
        /// A function which when passed an object of <typeparamref name="T"/> returns a control.
        /// </param>
        /// <param name="itemsSelector">
        /// A function which when passed an object of <typeparamref name="T"/> returns the child
        /// items.
        /// </param>
        /// <param name="isExpanded">
        /// A function which when passed an object of <typeparamref name="T"/> returns the the
        /// initial expanded state of the node.
        /// </param>
        public TreeDataTemplate(
            Func<T, Control> build,
            Func<T, IEnumerable> itemsSelector,
            Func<T, bool> isExpanded)
            : base(
                typeof(T),
                TreeDataTemplate<T>.Cast(build),
                TreeDataTemplate<T>.Cast(itemsSelector),
                TreeDataTemplate<T>.Cast(isExpanded))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TreeDataTemplate{T}"/> class.
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
            Func<T, bool> match,
            Func<T, Control> build,
            Func<T, IEnumerable> itemsSelector)
            : base(
                TreeDataTemplate<T>.CastMatch(match),
                TreeDataTemplate<T>.Cast(build),
                TreeDataTemplate<T>.Cast(itemsSelector))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TreeDataTemplate{T}"/> class.
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
            Func<T, bool> match,
            Func<T, Control> build,
            Func<T, IEnumerable> itemsSelector,
            Func<T, bool> isExpanded)
            : base(
                TreeDataTemplate<T>.CastMatch(match),
                TreeDataTemplate<T>.Cast(build),
                TreeDataTemplate<T>.Cast(itemsSelector),
                TreeDataTemplate<T>.Cast(isExpanded))
        {
        }

        /// <summary>
        /// Casts a typed match function to an untyped match function.
        /// </summary>
        /// <param name="f">The typed function.</param>
        /// <returns>The untyped function.</returns>
        private static Func<object, bool> CastMatch(Func<T, bool> f)
        {
            return o => (o is T) ? f((T)o) : false;
        }

        /// <summary>
        /// Casts a function with a typed parameter to an untyped function.
        /// </summary>
        /// <typeparam name="TResult">The result.</typeparam>
        /// <param name="f">The typed function.</param>
        /// <returns>The untyped function.</returns>
        private static Func<object, TResult> Cast<TResult>(Func<T, TResult> f)
        {
            return o => f((T)o);
        }
    }
}
