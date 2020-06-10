using System;
using Avalonia.Controls;
using Avalonia.Input;

namespace Avalonia.Platform
{
    /// <summary>
    /// Defines a platform-specific window implementation.
    /// </summary>
    public interface IWindowImpl : IWindowBaseImpl
    {
        /// <summary>
        /// Gets or sets the minimized/maximized state of the window.
        /// </summary>
        WindowState WindowState { get; set; }

        /// <summary>
        /// Gets or sets a method called when the minimized/maximized state of the window changes.
        /// </summary>
        Action<WindowState> WindowStateChanged { get; set; }

        /// <summary>
        /// Sets the title of the window.
        /// </summary>
        /// <param name="title">The title.</param>
        void SetTitle(string title);

        /// <summary>
        /// Sets the parent of the window.
        /// </summary>
        /// <param name="parent">The parent <see cref="IWindowImpl"/>.</param>
        void SetParent(IWindowImpl parent);
        
        /// <summary>
        /// Disables the window for example when a modal dialog is open.
        /// </summary>
        /// <param name="enable">true if the window is enabled, or false if it is disabled.</param>
        void SetEnabled(bool enable);

        /// <summary>
        /// Called when a disabled window received input. Can be used to activate child windows.
        /// </summary>
        Action GotInputWhenDisabled { get; set; }        

        /// <summary>
        /// Enables or disables system window decorations (title bar, buttons, etc)
        /// </summary>
        void SetSystemDecorations(SystemDecorations enabled);

        /// <summary>
        /// Sets the icon of this window.
        /// </summary>
        void SetIcon(IWindowIconImpl icon);

        /// <summary>
        /// Enables or disables the taskbar icon
        /// </summary>
        void ShowTaskbarIcon(bool value);

        /// <summary>
        /// Enables or disables resizing of the window
        /// </summary>
        void CanResize(bool value);

        /// <summary>
        /// Gets or sets a method called before the underlying implementation is destroyed.
        /// Return true to prevent the underlying implementation from closing.
        /// </summary>
        Func<bool> Closing { get; set; }

        /// <summary>
        /// Starts moving a window with left button being held. Should be called from left mouse button press event handler.
        /// </summary>
        void BeginMoveDrag(PointerPressedEventArgs e);

        /// <summary>
        /// Starts resizing a window. This function is used if an application has window resizing controls. 
        /// Should be called from left mouse button press event handler
        /// </summary>
        void BeginResizeDrag(WindowEdge edge, PointerPressedEventArgs e);

        /// <summary>
        /// Sets the client size of the top level.
        /// </summary>
        void Resize(Size clientSize);

        /// <summary>
        /// Sets the client size of the top level.
        /// </summary>
        void Move(PixelPoint point);

        /// <summary>
        /// Minimum width of the window.
        /// </summary>
        /// 
        void SetMinMaxSize(Size minSize, Size maxSize);

        void SetExtendClientAreaToDecorationsHint(bool extendIntoClientAreaHint);

        bool IsClientAreaExtendedToDecorations { get; }

        void SetExtendClientAreaChromeHints(ExtendClientAreaChromeHints hints);

        void SetExtendClientAreaTitleBarHeightHint(double titleBarHeight);

        Action<bool> ExtendClientAreaToDecorationsChanged { get; set; }
        
        Thickness ExtendedMargins { get; }

        Thickness OffScreenMargin { get; } 
    }
}
