// -----------------------------------------------------------------------
// <copyright file="DataTemplate.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using System.Reflection;

    public class DataTemplate
    {
        public static readonly DataTemplate Default =
            new DataTemplate(typeof(object), o => new TextBox { Text = o.ToString() });

        public DataTemplate(Type type, Func<object, IVisual> build)
            : this(o => type.GetTypeInfo().IsAssignableFrom(o.GetType().GetTypeInfo()), build)
        {
        }

        public DataTemplate(Func<object, bool> match, Func<object, IVisual> build)
        {
            Contract.Requires<ArgumentNullException>(match != null);
            Contract.Requires<ArgumentNullException>(build != null);

            this.Match = match;
            this.Build = build;
        }

        public Func<object, bool> Match { get; private set; }

        public Func<object, IVisual> Build { get; private set; }
    }

    public class DataTemplate<T> : DataTemplate
    {
        public DataTemplate(Func<T, IVisual> build)
            : base(typeof(T), o => build((T)o))
        {
        }

        public DataTemplate(Func<T, bool> match, Func<T, IVisual> build)
            : base(o => (o is T) ? match((T)o) : false, o => build((T)o))
        {
        }
    }
}
