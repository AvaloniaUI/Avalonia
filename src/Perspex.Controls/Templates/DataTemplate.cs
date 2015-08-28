﻿// -----------------------------------------------------------------------
// <copyright file="DataTemplate.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.Templates
{
    using System;
    using System.Reflection;

    /// <summary>
    /// Builds a control for a piece of data.
    /// </summary>
    public class DataTemplate : FuncTemplate<object, IControl>, IDataTemplate
    {
        /// <summary>
        /// The default data template used in the case where not matching data template is found.
        /// </summary>
        public static readonly DataTemplate Default =
           new DataTemplate(typeof(object), o => (o != null) ? new TextBlock { Text = o.ToString() } : null);

        /// <summary>
        /// The implementation of the <see cref="Match"/> method.
        /// </summary>
        private Func<object, bool> match;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataTemplate"/> class.
        /// </summary>
        /// <param name="type">The type of data which the data template matches.</param>
        /// <param name="build">
        /// A function which when passed an object of <paramref name="type"/> returns a control.
        /// </param>
        public DataTemplate(Type type, Func<object, IControl> build)
            : this(o => IsInstance(o, type), build)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataTemplate"/> class.
        /// </summary>
        /// <param name="match">
        /// A function which determines whether the data template matches the specified data.
        /// </param>
        /// <param name="build">
        /// A function which returns a control for matching data.
        /// </param>
        public DataTemplate(Func<object, bool> match, Func<object, IControl> build)
            : base(build)
        {
            Contract.Requires<ArgumentNullException>(match != null);

            this.match = match;
        }

        /// <summary>
        /// Checks to see if this data template matches the specified data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>
        /// True if the data template can build a control for the data, otherwise false.
        /// </returns>
        public bool Match(object data)
        {
            return this.match(data);
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