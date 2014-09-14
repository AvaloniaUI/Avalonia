// -----------------------------------------------------------------------
// <copyright file="TreeDataTemplate.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using System.Collections;

    public class TreeDataTemplate : DataTemplate
    {
        public TreeDataTemplate(
            Func<object, Control> build, 
            Func<object, IEnumerable> itemsSelector)
            : this(o => true, build, itemsSelector)
        {
        }

        public TreeDataTemplate(
            Type type, 
            Func<object, Control> build,
            Func<object, IEnumerable> itemsSelector)
            : this(o => IsInstance(o, type), build, itemsSelector)
        {
        }

        public TreeDataTemplate(
            Func<object, bool> match, 
            Func<object, Control> build,
            Func<object, IEnumerable> itemsSelector)
            : base(match, build)
        {
            this.ItemsSelector = itemsSelector;
        }

        public Func<object, IEnumerable> ItemsSelector { get; private set; }
    }

    public class TreeDataTemplate<T> : TreeDataTemplate
    {
        public TreeDataTemplate(
            Func<T, Control> build,
            Func<T, IEnumerable> itemsSelector)
            : base(typeof(T), o => build((T)o), o => itemsSelector((T)o))
        {
        }

        public TreeDataTemplate(
            Func<T, bool> match, 
            Func<T, Control> build,
            Func<T, IEnumerable> itemsSelector)
            : base(o => (o is T) ? match((T)o) : false, o => build((T)o), o => itemsSelector((T)o))
        {
        }
    }
}
