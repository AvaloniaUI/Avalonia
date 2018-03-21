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
using System.Linq;
using JetBrains.Annotations;
using System.ComponentModel;

namespace Avalonia.Controls
{
    /// <summary>
    /// Determines how a <see cref="Window"/> will size itself to fit its content.
    /// </summary>
    [Flags]
    public enum SizeToContent
    {
        /// <summary>
        /// The window will not automatically size itself to fit its content.
        /// </summary>
        Manual = 0,

        /// <summary>
        /// The window will size itself horizontally to fit its content.
        /// </summary>
        Width = 1,

        /// <summary>
        /// The window will size itself vertically to fit its content.
        /// </summary>
        Height = 2,

        /// <summary>
        /// The window will size itself horizontally and vertically to fit its content.
        /// </summary>
        WidthAndHeight = 3,
    }

    /// <summary>
    /// A top-level window.
    /// </summary>
    public class Window : WindowBase, IStyleable, IFocusScope, ILayoutRoot, INameScope
    {
        private static List<Window> s_windows = new List<Window>();

        /// <summary>
        /// Retrieves an enumeration of all Windows in the currently running application.
        /// </summary>
        public static IReadOnlyList<Window> OpenWindows => s_windows;

        /// <summary>
        /// Defines the <see cref="SizeToContent"/> property.
        /// </summary>
        public static readonly StyledProperty<SizeToContent> SizeToContentProperty =
            AvaloniaProperty.Register<Window, SizeToContent>(nameof(SizeToContent));

        /// <summary>
        /// Enables or disables system window decorations (title bar, buttons, etc)
        /// </summary>
        public static readonly StyledProperty<bool> HasSystemDecorationsProperty =
            AvaloniaProperty.Register<Window, bool>(nameof(HasSystemDecorations), true);
        
        /// <summary>
        /// Enables or disables the taskbar icon
        /// </summary>
        public static readonly StyledProperty<bool> ShowInTaskbarProperty =
            AvaloniaProperty.Register<Window, bool>(nameof(ShowInTaskbar), true);
        
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

        /// <summary>
        /// Defines the <see cref="WindowStartupLocation"/> proeprty.
        /// </summary>
        public static readonly DirectProperty<Window, WindowStartupLocation> WindowStartupLocationProperty =
            AvaloniaProperty.RegisterDirect<Window, WindowStartupLocation>(
                nameof(WindowStartupLocation),
                o => o.WindowStartupLocation,
                (o, v) => o.WindowStartupLocation = v);

        private readonly NameScope _nameScope = new NameScope();
        private object _dialogResult;
        private readonly Size _maxPlatformClientSize;
        private WindowStartupLocation _windowStartupLoction;

        /// <summary>
        /// Initializes static members of the <see cref="Window"/> class.
        /// </summary>
        static Window()
        {
            BackgroundProperty.OverrideDefaultValue(typeof(Window), Brushes.White);
            TitleProperty.Changed.AddClassHandler<Window>((s, e) => s.PlatformImpl?.SetTitle((string)e.NewValue));
            HasSystemDecorationsProperty.Changed.AddClassHandler<Window>(
                (s, e) => s.PlatformImpl?.SetSystemDecorations((bool) e.NewValue));

            ShowInTaskbarProperty.Changed.AddClassHandler<Window>((w, e) => w.PlatformImpl?.ShowTaskbarIcon((bool)e.NewValue));

            IconProperty.Changed.AddClassHandler<Window>((s, e) => s.PlatformImpl?.SetIcon(((WindowIcon)e.NewValue).PlatformImpl));
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
            impl.Closing = HandleClosing;
            _maxPlatformClientSize = PlatformImpl?.MaxClientSize ?? default(Size);
            Screens = new Screens(PlatformImpl?.Screen);
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

        public Screens Screens { get; private set; }

        /// <summary>
        /// Gets the platform-specific window implementation.
        /// </summary>
        [CanBeNull]
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
        /// Enables or disables system window decorations (title bar, buttons, etc)
        /// </summary>
        /// 
        public bool HasSystemDecorations
        {
            get { return GetValue(HasSystemDecorationsProperty); }
            set { SetValue(HasSystemDecorationsProperty, value); }
        }
        
        /// <summary>
        /// Enables or disables the taskbar icon
        /// </summary>
        /// 
        public bool ShowInTaskbar
        {
            get { return GetValue(ShowInTaskbarProperty); }
            set { SetValue(ShowInTaskbarProperty, value); }
        }

        /// <summary>
        /// Gets or sets the minimized/maximized state of the window.
        /// </summary>
        public WindowState WindowState
        {
            get { return PlatformImpl?.WindowState ?? WindowState.Normal; }
            set
            {
                if (PlatformImpl != null)
                    PlatformImpl.WindowState = value;
            }
        }

        /// <summary>
        /// Gets or sets the icon of the window.
        /// </summary>
        public WindowIcon Icon
        {
            get { return GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }

        /// <summary>
        /// Gets or sets the startup location of the window.
        /// </summary>
        public WindowStartupLocation WindowStartupLocation
        {
            get { return _windowStartupLoction; }
            set { SetAndRaise(WindowStartupLocationProperty, ref _windowStartupLoction, value); }
        }

        /// <inheritdoc/>
        Size ILayoutRoot.MaxClientSize => _maxPlatformClientSize;

        /// <inheritdoc/>
        Type IStyleable.StyleKey => typeof(Window);

        /// <summary>
        /// Fired before a window is closed.
        /// </summary>
        public event EventHandler<CancelEventArgs> Closing;

        /// <summary>
        /// Closes the window.
        /// </summary>
        public void Close()
        {
            Close(false);
        }

        protected override void HandleApplicationExiting()
        {
            base.HandleApplicationExiting();
            Close(true);
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
            Close(false);
        }

        internal void Close(bool ignoreCancel)
        {
            var cancelClosing = false;
            try
            {
                cancelClosing = HandleClosing();
            }
            finally
            {
                if (ignoreCancel || !cancelClosing)
                {
                    s_windows.Remove(this);
                    PlatformImpl?.Dispose();
                    IsVisible = false;
                }
            }
        }

        /// <summary>
        /// Handles a closing notification from <see cref="IWindowImpl.Closing"/>.
        /// </summary>
        protected virtual bool HandleClosing()
        {
            var args = new CancelEventArgs();
            Closing?.Invoke(this, args);
            return args.Cancel;
        }

        /// <summary>
        /// Hides the window but does not close it.
        /// </summary>
        public override void Hide()
        {
            if (!IsVisible)
            {
                return;
            }

            using (BeginAutoSizing())
            {
                Renderer?.Stop();
                PlatformImpl?.Hide();
            }

            IsVisible = false;
        }

        /// <summary>
        /// Shows the window.
        /// </summary>
        public override void Show()
        {
            if (IsVisible)
            {
                return;
            }

            s_windows.Add(this);

            EnsureInitialized();
            SetWindowStartupLocation();
            IsVisible = true;
            LayoutManager.Instance.ExecuteInitialLayoutPass(this);

            using (BeginAutoSizing())
            {
                PlatformImpl?.Show();
                Renderer?.Start();
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
            if (IsVisible)
            {
                throw new InvalidOperationException("The window is already being shown.");
            }

            s_windows.Add(this);

            EnsureInitialized();
            SetWindowStartupLocation();
            IsVisible = true;
            LayoutManager.Instance.ExecuteInitialLayoutPass(this);

            using (BeginAutoSizing())
            {
                var affectedWindows = s_windows.Where(w => w.IsEnabled && w != this).ToList();
                var activated = affectedWindows.Where(w => w.IsActive).FirstOrDefault();
                SetIsEnabled(affectedWindows, false);

                var modal = PlatformImpl?.ShowDialog();
                var result = new TaskCompletionSource<TResult>();

                Renderer?.Start();

                Observable.FromEventPattern<EventHandler, EventArgs>(
                    x => this.Closed += x,
                    x => this.Closed -= x)
                    .Take(1)
                    .Subscribe(_ =>
                    {
                        modal?.Dispose();
                        SetIsEnabled(affectedWindows, true);
                        activated?.Activate();
                        result.SetResult((TResult)(_dialogResult ?? default(TResult)));
                    });

                return result.Task;
            }
        }

        void SetIsEnabled(IEnumerable<Window> windows, bool isEnabled)
        {
            foreach (var window in windows)
            {
                window.IsEnabled = isEnabled;
            }
        }

        void SetWindowStartupLocation()
        {
            if (WindowStartupLocation == WindowStartupLocation.CenterScreen)
            {
                var screen = Screens.ScreenFromPoint(Bounds.Position);

                if (screen != null)
                    Position = screen.WorkingArea.CenterRect(new Rect(ClientSize)).Position;
            }
            else if (WindowStartupLocation == WindowStartupLocation.CenterOwner)
            {
                if (Owner != null)
                {
                    var positionAsSize = Owner.ClientSize / 2 - ClientSize / 2;
                    Position = Owner.Position + new Point(positionAsSize.Width, positionAsSize.Height);
                }
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
            var clientSize = ClientSize;
            Size constraint = clientSize;

            if ((sizeToContent & SizeToContent.Width) != 0)
            {
                constraint = constraint.WithWidth(double.PositiveInfinity);
            }

            if ((sizeToContent & SizeToContent.Height) != 0)
            {
                constraint = constraint.WithHeight(double.PositiveInfinity);
            }

            var result = base.MeasureOverride(constraint);

            if ((sizeToContent & SizeToContent.Width) == 0)
            {
                result = result.WithWidth(clientSize.Width);
            }

            if ((sizeToContent & SizeToContent.Height) == 0)
            {
                result = result.WithHeight(clientSize.Height);
            }

            return result;
        }

        protected override void HandleClosed()
        {
            IsVisible = false;
            s_windows.Remove(this);
            base.HandleClosed();
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
