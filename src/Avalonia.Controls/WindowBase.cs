using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Platform;
using JetBrains.Annotations;

namespace Avalonia.Controls
{
    /// <summary>
    /// Base class for top-level windows.
    /// </summary>
    /// <remarks>
    /// This class acts as a base for top level windows such as <see cref="Window"/> and
    /// <see cref="PopupRoot"/>. It handles scheduling layout, styling and rendering as well as
    /// tracking the window <see cref="TopLevel.ClientSize"/> and <see cref="IsActive"/> state.
    /// </remarks>
    public class WindowBase : TopLevel
    {
        /// <summary>
        /// Defines the <see cref="IsActive"/> property.
        /// </summary>
        public static readonly DirectProperty<WindowBase, bool> IsActiveProperty =
            AvaloniaProperty.RegisterDirect<WindowBase, bool>(nameof(IsActive), o => o.IsActive);

        /// <summary>
        /// Defines the <see cref="Owner"/> property.
        /// </summary>
        public static readonly DirectProperty<WindowBase, WindowBase> OwnerProperty =
            AvaloniaProperty.RegisterDirect<WindowBase, WindowBase>(
                nameof(Owner),
                o => o.Owner,
                (o, v) => o.Owner = v);

        public static readonly StyledProperty<bool> TopmostProperty =
            AvaloniaProperty.Register<WindowBase, bool>(nameof(Topmost));

        private bool _hasExecutedInitialLayoutPass;
        private bool _isActive;
        private bool _ignoreVisibilityChange;
        private WindowBase _owner;

        static WindowBase()
        {
            IsVisibleProperty.OverrideDefaultValue<WindowBase>(false);
            IsVisibleProperty.Changed.AddClassHandler<WindowBase>(x => x.IsVisibleChanged);

            MinWidthProperty.Changed.AddClassHandler<WindowBase>((w, e) => w.PlatformImpl?.SetMinMaxSize(new Size((double)e.NewValue, w.MinHeight), new Size(w.MaxWidth, w.MaxHeight)));
            MinHeightProperty.Changed.AddClassHandler<WindowBase>((w, e) => w.PlatformImpl?.SetMinMaxSize(new Size(w.MinWidth, (double)e.NewValue), new Size(w.MaxWidth, w.MaxHeight)));
            MaxWidthProperty.Changed.AddClassHandler<WindowBase>((w, e) => w.PlatformImpl?.SetMinMaxSize(new Size(w.MinWidth, w.MinHeight), new Size((double)e.NewValue, w.MaxHeight)));
            MaxHeightProperty.Changed.AddClassHandler<WindowBase>((w, e) => w.PlatformImpl?.SetMinMaxSize(new Size(w.MinWidth, w.MinHeight), new Size(w.MaxWidth, (double)e.NewValue)));
            
            TopmostProperty.Changed.AddClassHandler<WindowBase>((w, e) => w.PlatformImpl?.SetTopmost((bool)e.NewValue));
        }

        public WindowBase(IWindowBaseImpl impl) : this(impl, AvaloniaLocator.Current)
        {
        }

        public WindowBase(IWindowBaseImpl impl, IAvaloniaDependencyResolver dependencyResolver) : base(impl, dependencyResolver)
        {
            impl.Activated = HandleActivated;
            impl.Deactivated = HandleDeactivated;
            impl.PositionChanged = HandlePositionChanged;
            this.GetObservable(ClientSizeProperty).Skip(1).Subscribe(x => PlatformImpl?.Resize(x));
        }

        /// <summary>
        /// Fired when the window is activated.
        /// </summary>
        public event EventHandler Activated;

        /// <summary>
        /// Fired when the window is deactivated.
        /// </summary>
        public event EventHandler Deactivated;

        /// <summary>
        /// Fired when the window position is changed.
        /// </summary>
        public event EventHandler<PixelPointEventArgs> PositionChanged;

        [CanBeNull]
        public new IWindowBaseImpl PlatformImpl => (IWindowBaseImpl) base.PlatformImpl;

        /// <summary>
        /// Gets a value that indicates whether the window is active.
        /// </summary>
        public bool IsActive
        {
            get { return _isActive; }
            private set { SetAndRaise(IsActiveProperty, ref _isActive, value); }
        }

        /// <summary>
        /// Gets or sets the window position in screen coordinates.
        /// </summary>
        public PixelPoint Position
        {
            get { return PlatformImpl?.Position ?? PixelPoint.Origin; }
            set
            {
                if (PlatformImpl is IWindowBaseImpl impl)
                    impl.Position = value;
            }
        }

        /// <summary>
        /// Whether an auto-size operation is in progress.
        /// </summary>
        protected bool AutoSizing
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the owner of the window.
        /// </summary>
        public WindowBase Owner
        {
            get { return _owner; }
            set { SetAndRaise(OwnerProperty, ref _owner, value); }
        }

        /// <summary>
        /// Gets or sets whether this window appears on top of all other windows
        /// </summary>
        public bool Topmost
        {
            get { return GetValue(TopmostProperty); }
            set { SetValue(TopmostProperty, value); }
        }

        /// <summary>
        /// Activates the window.
        /// </summary>
        public void Activate()
        {
            PlatformImpl?.Activate();
        }

        /// <summary>
        /// Hides the popup.
        /// </summary>
        public virtual void Hide()
        {
            _ignoreVisibilityChange = true;

            try
            {
                Renderer?.Stop();
                PlatformImpl?.Hide();
                IsVisible = false;
            }
            finally
            {
                _ignoreVisibilityChange = false;
            }
        }

        /// <summary>
        /// Shows the window.
        /// </summary>
        public virtual void Show()
        {
            _ignoreVisibilityChange = true;

            try
            {
                EnsureInitialized();
                IsVisible = true;

                if (!_hasExecutedInitialLayoutPass)
                {
                    LayoutManager.ExecuteInitialLayoutPass(this);
                    _hasExecutedInitialLayoutPass = true;
                }
                PlatformImpl?.Show();
                Renderer?.Start();
                OnOpened(EventArgs.Empty);
            }
            finally
            {
                _ignoreVisibilityChange = false;
            }
        }

        /// <summary>
        /// Begins an auto-resize operation.
        /// </summary>
        /// <returns>A disposable used to finish the operation.</returns>
        /// <remarks>
        /// When an auto-resize operation is in progress any resize events received will not be
        /// cause the new size to be written to the <see cref="Layoutable.Width"/> and
        /// <see cref="Layoutable.Height"/> properties.
        /// </remarks>
        protected IDisposable BeginAutoSizing()
        {
            AutoSizing = true;
            return Disposable.Create(() => AutoSizing = false);
        }

        /// <summary>
        /// Carries out the arrange pass of the window.
        /// </summary>
        /// <param name="finalSize">The final window size.</param>
        /// <returns>The <paramref name="finalSize"/> parameter unchanged.</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            using (BeginAutoSizing())
            {
                PlatformImpl?.Resize(finalSize);
            }

            return base.ArrangeOverride(PlatformImpl?.ClientSize ?? default(Size));
        }

        /// <summary>
        /// Ensures that the window is initialized.
        /// </summary>
        protected void EnsureInitialized()
        {
            if (!IsInitialized)
            {
                var init = (ISupportInitialize)this;
                init.BeginInit();
                init.EndInit();
            }
        }

        protected override void HandleClosed()
        {
            _ignoreVisibilityChange = true;

            try
            {
                IsVisible = false;
                base.HandleClosed();
            }
            finally
            {
                _ignoreVisibilityChange = false;
            }
        }

        /// <summary>
        /// Handles a resize notification from <see cref="ITopLevelImpl.Resized"/>.
        /// </summary>
        /// <param name="clientSize">The new client size.</param>
        protected override void HandleResized(Size clientSize)
        {
            if (!AutoSizing)
            {
                Width = clientSize.Width;
                Height = clientSize.Height;
            }
            ClientSize = clientSize;
            LayoutManager.ExecuteLayoutPass();
            Renderer?.Resized(clientSize);
        }

        /// <summary>
        /// Handles a window position change notification from 
        /// <see cref="IWindowBaseImpl.PositionChanged"/>.
        /// </summary>
        /// <param name="pos">The window position.</param>
        private void HandlePositionChanged(PixelPoint pos)
        {
            PositionChanged?.Invoke(this, new PixelPointEventArgs(pos));
        }

        /// <summary>
        /// Handles an activated notification from <see cref="IWindowBaseImpl.Activated"/>.
        /// </summary>
        private void HandleActivated()
        {
            Activated?.Invoke(this, EventArgs.Empty);

            var scope = this as IFocusScope;

            if (scope != null)
            {
                FocusManager.SetFocusScope(scope);
            }

            IsActive = true;
        }

        /// <summary>
        /// Handles a deactivated notification from <see cref="IWindowBaseImpl.Deactivated"/>.
        /// </summary>
        private void HandleDeactivated()
        {
            IsActive = false;

            Deactivated?.Invoke(this, EventArgs.Empty);
        }

        private void IsVisibleChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (!_ignoreVisibilityChange)
            {
                if ((bool)e.NewValue)
                {
                    Show();
                }
                else
                {
                    Hide();
                }
            }
        }

        /// <summary>
        /// Starts moving a window with left button being held. Should be called from left mouse button press event handler
        /// </summary>
        public void BeginMoveDrag() => PlatformImpl?.BeginMoveDrag();

        /// <summary>
        /// Starts resizing a window. This function is used if an application has window resizing controls. 
        /// Should be called from left mouse button press event handler
        /// </summary>
        public void BeginResizeDrag(WindowEdge edge) => PlatformImpl?.BeginResizeDrag(edge);
    }
}
