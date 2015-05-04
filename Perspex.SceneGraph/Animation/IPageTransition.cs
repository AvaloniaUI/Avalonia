// -----------------------------------------------------------------------
// <copyright file="IPageTransition.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Animation
{
    using System.Threading.Tasks;

    public interface IPageTransition
    {
        Task Start(Visual from, Visual to, bool forward);
    }
}
