// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections;

namespace Avalonia.Controls.Templates
{
    /// <summary>
    /// A template used to build hierarchical data.
    /// </summary>
    /// <typeparam name="T">The type of the template's data.</typeparam>
    public class FuncTreeDataTemplate<T> : FuncTreeDataTemplate
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FuncTreeDataTemplate{T}"/> class.
        /// </summary>
        /// <param name="build">
        /// A function which when passed an object of <typeparamref name="T"/> returns a control.
        /// </param>
        /// <param name="itemsSelector">
        /// A function which when passed an object of <typeparamref name="T"/> returns the child
        /// items.
        /// </param>
        public FuncTreeDataTemplate(
            Func<T, INameScope, Control> build,
            Func<T, IEnumerable> itemsSelector)
            : base(
                typeof(T),
                Cast(build),
                Cast(itemsSelector))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FuncTreeDataTemplate{T}"/> class.
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
            Func<T, bool> match,
            Func<T, INameScope, Control> build,
            Func<T, IEnumerable> itemsSelector)
            : base(
                CastMatch(match),
                Cast(build),
                Cast(itemsSelector))
        {
        }

        /// <summary>
        /// Casts a typed match function to an untyped match function.
        /// </summary>
        /// <param name="f">The typed function.</param>
        /// <returns>The untyped function.</returns>
        private static Func<object, bool> CastMatch(Func<T, bool> f)
        {
            return o => (o is T) && f((T)o);
        }

        /// <summary>
        /// Casts a function with a typed parameter to an untyped function.
        /// </summary>
        /// <typeparam name="TResult">The result.</typeparam>
        /// <param name="f">The typed function.</param>
        /// <returns>The untyped function.</returns>
        private static Func<object, INameScope, TResult> Cast<TResult>(Func<T, INameScope, TResult> f)
        {
            return (o, s) => f((T)o, s);
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
