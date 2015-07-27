// -----------------------------------------------------------------------
// <copyright file="DataTemplate`1.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.Templates
{
    using System;

    /// <summary>
    /// Builds a control for a piece of data of specified type.
    /// </summary>
    /// <typeparam name="T">The type of the template's data.</typeparam>
    public class DataTemplate<T> : DataTemplate
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataTemplate{T}"/> class.
        /// </summary>
        /// <param name="build">
        /// A function which when passed an object of <typeparamref name="T"/> returns a control.
        /// </param>
        public DataTemplate(Func<T, Control> build)
            : base(typeof(T), CastBuild(build))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataTemplate{T}"/> class.
        /// </summary>
        /// <param name="match">
        /// A function which determines whether the data template matches the specified data.
        /// </param>
        /// <param name="build">
        /// A function which when passed an object of <typeparamref name="T"/> returns a control.
        /// </param>
        public DataTemplate(Func<T, bool> match, Func<T, Control> build)
            : base(CastMatch(match), CastBuild(build))
        {
        }

        /// <summary>
        /// Casts a stongly typed match function to a weakly typed one.
        /// </summary>
        /// <param name="f">The strongly typed function.</param>
        /// <returns>The weakly typed function.</returns>
        private static Func<object, bool> CastMatch(Func<T, bool> f)
        {
            return o => (o is T) ? f((T)o) : false;
        }

        /// <summary>
        /// Casts a stongly typed build function to a weakly typed one.
        /// </summary>
        /// <typeparam name="TResult">The strong data type.</typeparam>
        /// <param name="f">The strongly typed function.</param>
        /// <returns>The weakly typed function.</returns>
        private static Func<object, TResult> CastBuild<TResult>(Func<T, TResult> f)
        {
            return o => f((T)o);
        }
    }
}