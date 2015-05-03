// -----------------------------------------------------------------------
// <copyright file="IPerspexPropertyBinding.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Diagnostics
{
    public interface IPerspexPropertyBinding
    {
        string Description { get; }

        int Priority { get; }

        object Value { get; }
    }
}