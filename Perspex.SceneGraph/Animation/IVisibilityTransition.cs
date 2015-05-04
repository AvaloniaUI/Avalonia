// -----------------------------------------------------------------------
// <copyright file="IVisibilityTransition.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Animation
{
    using System.Threading.Tasks;

    public interface IVisibilityTransition
    {
        Task Start(Visual from, Visual to);
    }
}
