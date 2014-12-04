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
            : this(o => DataTemplate.IsInstance(o, type), build, itemsSelector)
        {
        }

        public TreeDataTemplate(
            Type type,
            Func<object, Control> build,
            Func<object, IEnumerable> itemsSelector,
            Func<object, bool> isExpanded)
            : this(o => DataTemplate.IsInstance(o, type), build, itemsSelector, isExpanded)
        {
        }

        public TreeDataTemplate(
            Func<object, bool> match, 
            Func<object, Control> build,
            Func<object, IEnumerable> itemsSelector)
            : this(match, build, itemsSelector, _ => false)
        {
            this.ItemsSelector = itemsSelector;
        }

        public TreeDataTemplate(
            Func<object, bool> match,
            Func<object, Control> build,
            Func<object, IEnumerable> itemsSelector,
            Func<object, bool> isExpanded)
            : base(match, build)
        {
            this.ItemsSelector = itemsSelector;
            this.IsExpanded = isExpanded;
        }

        public Func<object, IEnumerable> ItemsSelector { get; private set; }

        public Func<object, bool> IsExpanded { get; private set; }
    }

    public class TreeDataTemplate<T> : TreeDataTemplate
    {
        public TreeDataTemplate(
            Func<T, Control> build,
            Func<T, IEnumerable> itemsSelector)
            : base(
                typeof(T), 
                TreeDataTemplate<T>.Cast(build), 
                TreeDataTemplate<T>.Cast(itemsSelector))
        {
        }

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

        private static Func<object, bool> CastMatch(Func<T, bool> f)
        {
            return o => (o is T) ? f((T)o) : false;
        }

        private static Func<object, TResult> Cast<TResult>(Func<T, TResult> f)
        {
            return o => f((T)o);
        }
    }
}
