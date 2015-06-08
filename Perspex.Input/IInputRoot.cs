// -----------------------------------------------------------------------
// <copyright file="IInputRoot.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Input
{
    /// <summary>
    /// Defines the interface for top-level input elements.
    /// </summary>
    public interface IInputRoot : IInputElement
    {
        /// <summary>
        /// Gets or sets the access key handler.
        /// </summary>
        IAccessKeyHandler AccessKeyHandler { get; }

        /// <summary>
        /// Gets or sets a value indicating whether access keys are shown in the window.
        /// </summary>
        bool ShowAccessKeys { get; set; }
    }
}
