// -----------------------------------------------------------------------
// <copyright file="IWindowImpl.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Platform
{
    using System;
    using System.Threading.Tasks;

    public interface IWindowImpl : ITopLevelImpl
    {
        void SetTitle(string title);

        void Show();

        IDisposable ShowDialog();

        void Hide();
    }
}
