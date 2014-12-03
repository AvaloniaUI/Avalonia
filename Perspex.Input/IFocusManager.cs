// -----------------------------------------------------------------------
// <copyright file="IFocusManager.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Input
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public interface IFocusManager
    {
        IInputElement Current { get; }

        IFocusScope Scope { get; }

        void Focus(IInputElement focusable);

        /// <summary>
        /// Notifies the focus manager of a change in focus scope.
        /// </summary>
        /// <param name="scope">The new focus scope.</param>
        /// <remarks>
        /// This should not be called by client code. It is called by an <see cref="IFocusScope"/>
        /// when it activates, e.g. when a Window is activated.
        /// </remarks>
        void SetFocusScope(IFocusScope scope);
    }
}
