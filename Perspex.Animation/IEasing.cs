// -----------------------------------------------------------------------
// <copyright file="PropertyTransition.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Animation
{
    public interface IEasing
    {
        object Ease(double progress, object start, object finish);
    }

    public interface IEasing<T> : IEasing
    {
        T Ease(double progress, T start, T finish);
    }
}
