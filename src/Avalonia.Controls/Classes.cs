// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Collections;

namespace Avalonia.Controls
{
    /// <summary>
    /// Holds a collection of style classes for an <see cref="IControl"/>.
    /// </summary>
    /// <remarks>
    /// Similar to CSS, each control may have any number of styling classes applied.
    /// </remarks>
    public class Classes : AvaloniaList<string>, IPseudoClasses
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Classes"/> class.
        /// </summary>
        public Classes()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Classes"/> class.
        /// </summary>
        /// <param name="items">The initial items.</param>
        public Classes(IEnumerable<string> items)
            : base(items)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Classes"/> class.
        /// </summary>
        /// <param name="items">The initial items.</param>
        public Classes(params string[] items)
            : base(items)
        {            
        }

        /// <summary>
        /// Adds a style class to the collection.
        /// </summary>
        /// <param name="name">The class name.</param>
        /// <remarks>
        /// Only standard classes may be added via this method. To add pseudoclasses (classes
        /// beginning with a ':' character) use the protected <see cref="Control.PseudoClasses"/>
        /// property.
        /// </remarks>
        public override void Add(string name)
        {
            ThrowIfPseudoclass(name, "added");

            if (!Contains(name))
            {
                base.Add(name);
            }
        }

        /// <summary>
        /// Adds a style classes to the collection.
        /// </summary>
        /// <param name="names">The class names.</param>
        /// <remarks>
        /// Only standard classes may be added via this method. To add pseudoclasses (classes
        /// beginning with a ':' character) use the protected <see cref="Control.PseudoClasses"/>
        /// property.
        /// </remarks>
        public override void AddRange(IEnumerable<string> names)
        {
            var c = new List<string>();

            foreach (var name in names)
            {
                ThrowIfPseudoclass(name, "added");

                if (!Contains(name))
                {
                    c.Add(name);
                }
            }

            base.AddRange(c);
        }

        /// <summary>
        /// Remvoes all non-pseudoclasses from the collection.
        /// </summary>
        public override void Clear()
        {
            for (var i = Count - 1; i >= 0; --i)
            {
                if (!this[i].StartsWith(":"))
                {
                    RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Inserts a style class into the collection.
        /// </summary>
        /// <param name="index">The index to insert the class at.</param>
        /// <param name="name">The class name.</param>
        /// <remarks>
        /// Only standard classes may be added via this method. To add pseudoclasses (classes
        /// beginning with a ':' character) use the protected <see cref="Control.PseudoClasses"/>
        /// property.
        /// </remarks>
        public override void Insert(int index, string name)
        {
            ThrowIfPseudoclass(name, "added");

            if (!Contains(name))
            {
                base.Insert(index, name);
            }
        }

        /// <summary>
        /// Inserts style classes into the collection.
        /// </summary>
        /// <param name="index">The index to insert the class at.</param>
        /// <param name="names">The class names.</param>
        /// <remarks>
        /// Only standard classes may be added via this method. To add pseudoclasses (classes
        /// beginning with a ':' character) use the protected <see cref="Control.PseudoClasses"/>
        /// property.
        /// </remarks>
        public override void InsertRange(int index, IEnumerable<string> names)
        {
            var c = new List<string>();

            foreach (var name in names)
            {
                ThrowIfPseudoclass(name, "added");

                if (!Contains(name))
                {
                    c.Add(name);
                }
            }

            base.InsertRange(index, c);
        }

        /// <summary>
        /// Removes a style class from the collection.
        /// </summary>
        /// <param name="name">The class name.</param>
        /// <remarks>
        /// Only standard classes may be removed via this method. To remove pseudoclasses (classes
        /// beginning with a ':' character) use the protected <see cref="Control.PseudoClasses"/>
        /// property.
        /// </remarks>
        public override bool Remove(string name)
        {
            ThrowIfPseudoclass(name, "removed");
            return base.Remove(name);
        }

        /// <summary>
        /// Removes style classes from the collection.
        /// </summary>
        /// <param name="names">The class name.</param>
        /// <remarks>
        /// Only standard classes may be removed via this method. To remove pseudoclasses (classes
        /// beginning with a ':' character) use the protected <see cref="Control.PseudoClasses"/>
        /// property.
        /// </remarks>
        public override void RemoveAll(IEnumerable<string> names)
        {
            var c = new List<string>();

            foreach (var name in names)
            {
                ThrowIfPseudoclass(name, "removed");

                if (Contains(name))
                {
                    c.Add(name);
                }
            }

            base.RemoveAll(c);
        }

        /// <summary>
        /// Removes a style class from the collection.
        /// </summary>
        /// <param name="index">The index of the class in the collection.</param>
        /// <remarks>
        /// Only standard classes may be removed via this method. To remove pseudoclasses (classes
        /// beginning with a ':' character) use the protected <see cref="Control.PseudoClasses"/>
        /// property.
        /// </remarks>
        public override void RemoveAt(int index)
        {
            var name = this[index];
            ThrowIfPseudoclass(name, "removed");
            base.RemoveAt(index);
        }

        /// <summary>
        /// Removes style classes from the collection.
        /// </summary>
        /// <param name="index">The first index to remove.</param>
        /// <param name="count">The number of items to remove.</param>
        public override void RemoveRange(int index, int count)
        {
            base.RemoveRange(index, count);
        }

        /// <summary>
        /// Removes all non-pseudoclasses in the collection and adds a new set.
        /// </summary>
        /// <param name="source">The new contents of the collection.</param>
        public void Replace(IList<string> source)
        {
            var toRemove = new List<string>();

            foreach (var name in source)
            {
                ThrowIfPseudoclass(name, "added");
            }

            foreach (var name in this)
            {
                if (!name.StartsWith(":"))
                {
                    toRemove.Add(name);
                }
            }

            base.RemoveAll(toRemove);
            base.AddRange(source);
        }

        /// <inheritdoc/>
        void IPseudoClasses.Add(string name)
        {
            if (!Contains(name))
            {
                base.Add(name);
            }
        }

        /// <inheritdoc/>
        bool IPseudoClasses.Remove(string name)
        {
            return base.Remove(name);
        }

        private void ThrowIfPseudoclass(string name, string operation)
        {
            if (name.StartsWith(":"))
            {
                throw new ArgumentException(
                    $"The pseudoclass '{name}' may only be {operation} by the control itself.");
            }
        }
    }
}
