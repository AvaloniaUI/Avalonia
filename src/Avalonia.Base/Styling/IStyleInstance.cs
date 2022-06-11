using System;
using Avalonia.Metadata;

namespace Avalonia.Styling
{
    /// <summary>
    /// Represents a style that has been instanced on a control.
    /// </summary>
    [Unstable]
    public interface IStyleInstance : IDisposable
    {
        /// <summary>
        /// Gets the source style.
        /// </summary>
        IStyle Source { get; }

        /// <summary>
        /// Gets a value indicating whether this style has an activator.
        /// </summary>
        bool HasActivator { get; }
        
        /// <summary>
        /// Gets a value indicating whether this style is active.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Instructs the style to start acting upon the control.
        /// </summary>
        void Start();
    }
}
