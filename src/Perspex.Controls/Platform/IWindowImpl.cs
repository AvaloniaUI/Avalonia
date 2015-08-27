// -----------------------------------------------------------------------
// <copyright file="IWindowImpl.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Platform
{
    using System;

    /// <summary>
    /// Defines a platform-specific window implementation.
    /// </summary>
    public interface IWindowImpl : ITopLevelImpl
    {
        /// <summary>
        /// Sets the title of the window.
        /// </summary>
        /// <param name="title">The title.</param>
        void SetTitle(string title);

        /// <summary>
        /// Shows the window.
        /// </summary>
        void Show();

        /// <summary>
        /// Shows the window as a dialog.
        /// </summary>
        /// <returns>
        /// An <see cref="IDisposable"/> that should be used to close the window.
        /// </returns>
        IDisposable ShowDialog();

        /// <summary>
        /// Hides the window.
        /// </summary>
        void Hide();
    }
}
