// -----------------------------------------------------------------------
// <copyright file="PointerPressEventArgs.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Input
{
    public class PointerPressEventArgs : PointerEventArgs
    {
        public int ClickCount { get; set; }
    }
}
