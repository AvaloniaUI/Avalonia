// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;
using System.Collections.Generic;

namespace Avalonia.Controls
{
    /// <summary>
    /// Determines how a <see cref="Window"/> will size itself to fit its content.
    /// </summary>
    public enum SizeToContent
    {
        /// <summary>
        /// The window will not automatically size itself to fit its content.
        /// </summary>
        Manual,

        /// <summary>
        /// The window will size itself horizontally to fit its content.
        /// </summary>
        Width,

        /// <summary>
        /// The window will size itself vertically to fit its content.
        /// </summary>
        Height,

        /// <summary>
        /// The window will size itself horizontally and vertically to fit its content.
        /// </summary>
        WidthAndHeight,
    }

    /// <summary>
    /// A top-level window.
    /// </summary>
    public class Window : TopLevel, IStyleable, IFocusScope, ILayoutRoot, INameScope
    {
        private static IList<Window> s_windows = new List<Window>();

        /// <summary>
        /// Retrieves an enumeration of all Windows in the currently running application.
        /// </summary>
        public static IList<Window> OpenWindows => s_windows;

        /// <summary>
        /// Defines the <see cref="SizeToContent"/> property.
        /// </summary>
        public static readonly StyledProperty<SizeToContent> SizeToContentProperty =
            AvaloniaProperty.Register<Window, SizeToContent>(nameof(SizeToContent));

        /// <summary>
        /// Enables of disables system window decorations (title bar, buttons, etc)
        /// </summary>
        public static readonly StyledProperty<bool> HasSystemDecorationsProperty =
            AvaloniaProperty.Register<Window, bool>(nameof(HasSystemDecorations), true);

        /// <summary>
        /// Sets if the window should cover the taskbar when maximized. Only applies to Windows 
        /// with HasSystemDecorations = false.
        /// </summary>
        public static readonly StyledProperty<bool> CoverTaskbarOnMaximizeProperty =
            AvaloniaProperty.Register<Window, bool>(nameof(CoverTaskbarOnMaximize), true);

        /// <summary>
        /// Defines the <see cref="Title"/> property.
        /// </summary>
        public static readonly StyledProperty<string> TitleProperty =
            AvaloniaProperty.Register<Window, string>(nameof(Title), "Window");

        /// <summary>
        /// Defines the <see cref="Icon"/> property.
        /// </summary>
        public static readonly StyledProperty<WindowIcon> IconProperty =
            AvaloniaProperty.Register<Window, WindowIcon>(nameof(Icon));

        private readonly NameScope _nameScope = new NameScope();
        private object _dialogResult;
        private readonly Size _maxPlatformClientSize;

        /// <summary>
        /// Initializes static members of the <see cref="Window"/> class.
        /// </summary>
        static Window()
        {
            BackgroundProperty.OverrideDefaultValue(typeof(Window), Brushes.White);
            TitleProperty.Changed.AddClassHandler<Window>((s, e) => s.PlatformImpl.SetTitle((string)e.NewValue));
            HasSystemDecorationsProperty.Changed.AddClassHandler<Window>(
                (s, e) => s.PlatformImpl.SetSystemDecorations((bool) e.NewValue));

            CoverTaskbarOnMaximizeProperty.Changed.AddClassHandler<Window>(
                (s, e) => s.PlatformImpl.SetCoverTaskbarWhenMaximized((bool)e.NewValue));

            IconProperty.Changed.AddClassHandler<Window>((s, e) => s.PlatformImpl.SetIcon(((WindowIcon)e.NewValue).PlatformImpl));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Window"/> class.
        /// </summary>
        public Window()
            : this(PlatformManager.CreateWindow())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Window"/> class.
        /// </summary>
        /// <param name="impl">The window implementation.</param>
        public Window(IWindowImpl impl)
            : base(impl)
        {
            _maxPlatformClientSize = this.PlatformImpl.MaxClientSize;
        }

        /// <inheritdoc/>
        event EventHandler<NameScopeEventArgs> INameScope.Registered
        {
            add { _nameScope.Registered += value; }
            remove { _nameScope.Registered -= value; }
        }

        /// <inheritdoc/>
        event EventHandler<NameScopeEventArgs> INameScope.Unregistered
        {
            add { _nameScope.Unregistered += value; }
            remove { _nameScope.Unregistered -= value; }
        }

        /// <summary>
        /// Gets the platform-specific window implementation.
        /// </summary>
        public new IWindowImpl PlatformImpl => (IWindowImpl)base.PlatformImpl;

        /// <summary>
        /// Gets or sets a value indicating how the window will size itself to fit its content.
        /// </summary>
        public SizeToContent SizeToContent
        {
            get { return GetValue(SizeToContentProperty); }
            set { SetValue(SizeToContentProperty, value); }
        }

        /// <summary>
        /// Gets or sets the title of the window.
        /// </summary>
        public string Title
        {
            get { return GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        /// <summary>
        /// Enables of disables system window decorations (title bar, buttons, etc)
        /// </summary>
        /// 
        public bool HasSystemDecorations
        {
            get { return GetValue(HasSystemDecorationsProperty); }
            set { SetValue(HasSystemDecorationsProperty, value); }
        }

        /// <summary>
        /// Sets if the window should cover the taskbar when maximized. Only applies to Windows 
        /// with HasSystemDecorations = false.
        /// </summary>
        public bool CoverTaskbarOnMaximize
        {
            get { return GetValue(CoverTaskbarOnMaximizeProperty); }
            set { SetValue(CoverTaskbarOnMaximizeProperty, value); }
        }

        /// <summary>
        /// Gets or sets the minimized/maximized state of the window.
        /// </summary>
        public WindowState WindowState
        {
            get { return this.PlatformImpl.WindowState; }
            set { this.PlatformImpl.WindowState = value; }
        }

        /// <summary>
        /// Gets or sets the icon of the window.
        /// </summary>
        public WindowIcon Icon
        {
            get { return GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }

        /// <inheritdoc/>
        Size ILayoutRoot.MaxClientSize => _maxPlatformClientSize;

        /// <inheritdoc/>
        Type IStyleable.StyleKey => typeof(Window);

        /// <summary>
        /// Closes the window.
        /// </summary>
        public void Close()
        {
            s_windows.Remove(this);
            PlatformImpl.Dispose();
        }

        protected override void HandleApplicationExiting()
        {
            base.HandleApplicationExiting();
            Close();
        }

        /// <summary>
        /// Closes a dialog window with the specified result.
        /// </summary>
        /// <param name="dialogResult">The dialog result.</param>
        /// <remarks>
        /// When the window is shown with the <see cref="ShowDialog{TResult}"/> method, the
        /// resulting task will produce the <see cref="_dialogResult"/> value when the window
        /// is closed.
        /// </remarks>
        public void Close(object dialogResult)
        {
            _dialogResult = dialogResult;
            Close();
        }

        /// <summary>
        /// Hides the window but does not close it.
        /// </summary>
        public void Hide()
        {
            using (BeginAutoSizing())
            {
                PlatformImpl.Hide();
            }
        }

        /// <summary>
        /// Shows the window.
        /// </summary>
        public void Show()
        {
            s_windows.Add(this);

            EnsureInitialized();
            LayoutManager.Instance.ExecuteInitialLayoutPass(this);

            using (BeginAutoSizing())
            {
                PlatformImpl.Show();
            }
        }

        /// <summary>
        /// Shows the window as a dialog.
        /// </summary>
        /// <returns>
        /// A task that can be used to track the lifetime of the dialog.
        /// </returns>
        public Task ShowDialog()
        {
            return ShowDialog<object>();
        }

        /// <summary>
        /// Shows the window as a dialog.
        /// </summary>
        /// <typeparam name="TResult">
        /// The type of the result produced by the dialog.
        /// </typeparam>
        /// <returns>.
        /// A task that can be used to retrive the result of the dialog when it closes.
        /// </returns>
        public Task<TResult> ShowDialog<TResult>()
        {
            s_windows.Add(this);

            EnsureInitialized();
            LayoutManager.Instance.ExecuteInitialLayoutPass(this);

            using (BeginAutoSizing())
            {
                var modal = PlatformImpl.ShowDialog();
                var result = new TaskCompletionSource<TResult>();

                Observable.FromEventPattern(this, nameof(Closed))
                    .Take(1)
                    .Subscribe(_ =>
                    {
                        modal.Dispose();
                        result.SetResult((TResult)_dialogResult);
                    });

                return result.Task;
            }
        }

        /// <inheritdoc/>
        void INameScope.Register(string name, object element)
        {
            _nameScope.Register(name, element);
        }

        /// <inheritdoc/>
        object INameScope.Find(string name)
        {
            return _nameScope.Find(name);
        }

        /// <inheritdoc/>
        void INameScope.Unregister(string name)
        {
            _nameScope.Unregister(name);
        }

        /// <inheritdoc/>
        protected override Size MeasureOverride(Size availableSize)
        {
            var sizeToContent = SizeToContent;
            var size = ClientSize;
            var desired = base.MeasureOverride(availableSize.Constrain(_maxPlatformClientSize));

            switch (sizeToContent)
            {
                case SizeToContent.Width:
                    size = new Size(desired.Width, ClientSize.Height);
                    break;
                case SizeToContent.Height:
                    size = new Size(ClientSize.Width, desired.Height);
                    break;
                case SizeToContent.WidthAndHeight:
                    size = new Size(desired.Width, desired.Height);
                    break;
                case SizeToContent.Manual:
                    size = ClientSize;
                    break;
                default:
                    throw new InvalidOperationException("Invalid value for SizeToContent.");
            }

            return size;
        }

        /// <inheritdoc/>
        protected override void HandleResized(Size clientSize)
        {
            if (!AutoSizing)
            {
                SizeToContent = SizeToContent.Manual;
            }

            base.HandleResized(clientSize);
        }

        private void EnsureInitialized()
        {
            if (!this.IsInitialized)
            {
                var init = (ISupportInitialize)this;
                init.BeginInit();
                init.EndInit();
            }
        }
    }
}

namespace Avalonia
{
    public static class WindowApplicationExtensions
    {
        public static void RunWithMainWindow<TWindow>(this Application app) where TWindow : Avalonia.Controls.Window, new()
        {
            var window = new TWindow();
            window.Show();
            app.Run(window);
        }
    }
}
