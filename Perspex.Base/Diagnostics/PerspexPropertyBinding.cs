// -----------------------------------------------------------------------
// <copyright file="PerspexPropertyBinding.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Diagnostics
{
    internal class PerspexPropertyBinding : IPerspexPropertyBinding
    {
        public string Description { get; set; }

        public int Priority { get; set; }

        public object Value { get; set; }
    }
}