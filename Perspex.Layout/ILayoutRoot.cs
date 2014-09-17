// -----------------------------------------------------------------------
// <copyright file="ILayoutRoot.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Layout
{
    public interface ILayoutRoot : ILayoutable
    {
        Size ClientSize { get; }

        ILayoutManager LayoutManager { get; }
    }
}
