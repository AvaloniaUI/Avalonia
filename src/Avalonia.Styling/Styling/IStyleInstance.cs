using System;

#nullable enable

namespace Avalonia.Styling
{
    /// <summary>
    /// Represents a style that has been instanced on a control.
    /// </summary>
    public interface IStyleInstance : IDisposable
    {
        /// <summary>
        /// Gets the source style.
        /// </summary>
        IStyle Source { get; }

        bool IsActive { get; }

        /// <summary>
        /// Instructs the style to start acting upon the control.
        /// </summary>
        void Start();
    }
}
