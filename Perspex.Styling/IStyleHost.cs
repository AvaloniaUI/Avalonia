// -----------------------------------------------------------------------
// <copyright file="IStyleHost.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Styling
{
    public interface IStyleHost : IVisual
    {
        Styles Styles { get; }
    }
}
