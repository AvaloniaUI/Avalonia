// -----------------------------------------------------------------------
// <copyright file="IInputManager.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Input
{
    using Perspex.Input.Raw;

    public interface IInputManager
    {
        void Process(RawInputEventArgs e);
    }
}
