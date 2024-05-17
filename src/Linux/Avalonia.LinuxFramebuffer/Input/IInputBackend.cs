using System;
using Avalonia.Input;
using Avalonia.Input.Raw;

namespace Avalonia.LinuxFramebuffer.Input
{
    /// <summary>
    ///  Base Input Backend signature.
    /// </summary>
    public interface IInputBackend
    {
        /// <summary>
        /// Initialize Input Backend
        /// </summary>
        /// <param name="info">screen info provider</param>
        /// <param name="onInput"><see cref="RawInputEventArgs"/> event dispatcher.</param>
        void Initialize(IScreenInfoProvider info, Action<RawInputEventArgs> onInput);
        
        /// <summary>
        /// Set Input Root
        /// </summary>
        /// <param name="root">Current root element</param>
        void SetInputRoot(IInputRoot root);
    }
}
