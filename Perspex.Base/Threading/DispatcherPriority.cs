// -----------------------------------------------------------------------
// <copyright file="DispatcherPriority.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Threading
{
    public enum DispatcherPriority
    {
        Invalid = -1,
        Inactive = 0,
        SystemIdle = 1,
        ApplicationIdle = 2,
        ContextIdle = 3,
        Background = 4,
        Input = 5,
        Loaded = 6,
        Render = 7,
        DataBind = 8,
        Normal = 9,
        Send = 10,
    }
}
