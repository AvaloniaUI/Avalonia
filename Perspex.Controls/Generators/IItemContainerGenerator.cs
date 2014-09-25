// -----------------------------------------------------------------------
// <copyright file="IItemContainerGenerator.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.Generators
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    public enum ItemContainerGeneratorState
    {
        NoStarted,
        Generating,
        Generated,
    }

    public interface IItemContainerGenerator
    {
        event EventHandler StateChanged;

        ItemContainerGeneratorState State { get; }

        Control GetContainerForItem(object item);

        object GetItemForContainer(Control container);

        IEnumerable<Tuple<object, Control>> GetAll();

        IEnumerable<Control> Generate(IEnumerable items);

        IEnumerable<Control> Remove(IEnumerable item);

        void RemoveAll();
    }
}
