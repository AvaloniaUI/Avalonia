﻿using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Automation.Peers;
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
        public static readonly DirectProperty<WindowBase, WindowBase?> OwnerProperty =
            AvaloniaProperty.RegisterDirect<WindowBase, WindowBase?>(
                nameof(Owner),
                o => o.Owner,
                (o, v) => o.Owner = v);

        public static readonly StyledProperty<bool> TopmostProperty =
            AvaloniaProperty.Register<WindowBase, bool>(nameof(Topmost));

        private bool _hasExecutedInitialLayoutPass;
        private bool _isActive;
        private bool _ignoreVisibilityChange;
        private WindowBase? _owner;

        static WindowBase()
        {
            IsVisibleProperty.OverrideDefaultValue<WindowBase>(false);
            IsVisibleProperty.Changed.AddClassHandler<WindowBase>((x,e) => x.IsVisibleChanged(e));

            
            TopmostProperty.Changed.AddClassHandler<WindowBase>((w, e) => w.PlatformImpl?.SetTopmost((bool)e.NewValue!));
        }

        public WindowBase(IWindowBaseImpl impl) : this(impl, AvaloniaLocator.Current)
        {
        }

        public WindowBase(IWindowBaseImpl impl, IAvaloniaDependencyResolver? dependencyResolver) : base(impl, dependencyResolver)
        {
            Screens = new Screens(impl.Screen);
            impl.Activated = HandleActivated;
            impl.Deactivated = HandleDeactivated;
            impl.PositionChanged = HandlePositionChanged;
        }

        /// <summary>
        /// Fired when the window is activated.
        /// </summary>
        public event EventHandler? Activated;

        /// <summary>
        /// Fired when the window is deactivated.
        /// </summary>
        public event EventHandler? Deactivated;

        /// <summary>
        /// Fired when the window position is changed.
        /// </summary>
        public event EventHandler<PixelPointEventArgs>? PositionChanged;

        public new IWindowBaseImpl? PlatformImpl => (IWindowBaseImpl?) base.PlatformImpl;

        /// <summary>
        /// Gets a value that indicates whether the window is active.
        /// </summary>
        public bool IsActive
        {
            get { return _isActive; }
            private set { SetAndRaise(IsActiveProperty, ref _isActive, value); }
        }
        
        public Screens Screens { get; private set; }

        [Obsolete("No longer used. Always returns false.")]
        protected bool AutoSizing => false;

        /// <summary>
        /// Gets or sets the owner of the window.
        /// </summary>
        public WindowBase? Owner
        {
            get { return _owner; }
            protected set { SetAndRaise(OwnerProperty, ref _owner, value); }
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
                    LayoutManager.ExecuteInitialLayoutPass();
                    _hasExecutedInitialLayoutPass = true;
                }
                PlatformImpl?.Show(true, false);
                Renderer?.Start();
                OnOpened(EventArgs.Empty);
            }
            finally
            {
                _ignoreVisibilityChange = false;
            }
        }

        [Obsolete("No longer used. Has no effect.")]
        protected IDisposable BeginAutoSizing() => Disposable.Empty;

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

        /// <inheritdoc/>
        protected override void OnClosed(EventArgs e)
        {
            // Window must manually raise Loaded/Unloaded events as it is a visual root and
            // does not raise OnAttachedToVisualTreeCore/OnDetachedFromVisualTreeCore events
            OnUnloadedCore();

            base.OnClosed(e);
        }

        /// <inheritdoc/>
        protected override void OnOpened(EventArgs e)
        {
            // Window must manually raise Loaded/Unloaded events as it is a visual root and
            // does not raise OnAttachedToVisualTreeCore/OnDetachedFromVisualTreeCore events
            ScheduleOnLoadedCore();

            base.OnOpened(e);
        }

        protected override void HandleClosed()
        {
            _ignoreVisibilityChange = true;

            try
            {
                IsVisible = false;
                
                if (this is IFocusScope scope)
                {
                    FocusManager.Instance?.RemoveFocusScope(scope);
                }
                
                base.HandleClosed();
            }
            finally
            {
                _ignoreVisibilityChange = false;
            }
        }

        [Obsolete("Use HandleResized(Size, PlatformResizeReason)")]
        protected override void HandleResized(Size clientSize) => HandleResized(clientSize, PlatformResizeReason.Unspecified);

        /// <summary>
        /// Handles a resize notification from <see cref="ITopLevelImpl.Resized"/>.
        /// </summary>
        /// <param name="clientSize">The new client size.</param>
        /// <param name="reason">The reason for the resize.</param>
        protected override void HandleResized(Size clientSize, PlatformResizeReason reason)
        {
            FrameSize = PlatformImpl?.FrameSize;

            if (ClientSize != clientSize)
            {
                ClientSize = clientSize;
                LayoutManager.ExecuteLayoutPass();
                Renderer?.Resized(clientSize);
            }
        }

        /// <summary>
        /// Overrides the core measure logic for windows.
        /// </summary>
        /// <param name="availableSize">The available size.</param>
        /// <returns>The measured size.</returns>
        /// <remarks>
        /// The layout logic for top-level windows is different than for other controls because
        /// they don't have a parent, meaning that many layout properties handled by the default
        /// MeasureCore (such as margins and alignment) make no sense.
        /// </remarks>
        protected override Size MeasureCore(Size availableSize)
        {
            ApplyStyling();
            ApplyTemplate();

            var constraint = LayoutHelper.ApplyLayoutConstraints(this, availableSize);

            return MeasureOverride(constraint);
        }

        /// <summary>
        /// Overrides the core arrange logic for windows.
        /// </summary>
        /// <param name="finalRect">The final arrange rect.</param>
        /// <remarks>
        /// The layout logic for top-level windows is different than for other controls because
        /// they don't have a parent, meaning that many layout properties handled by the default
        /// ArrangeCore (such as margins and alignment) make no sense.
        /// </remarks>
        protected override void ArrangeCore(Rect finalRect)
        {
            var constraint = ArrangeSetBounds(finalRect.Size);
            var arrangeSize = ArrangeOverride(constraint);
            Bounds = new Rect(arrangeSize);
        }

        /// <summary>
        /// Called during the arrange pass to set the size of the window.
        /// </summary>
        /// <param name="size">The requested size of the window.</param>
        /// <returns>The actual size of the window.</returns>
        protected virtual Size ArrangeSetBounds(Size size) => size;

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
                FocusManager.Instance?.SetFocusScope(scope);
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
                if ((bool)e.NewValue!)
                {
                    Show();
                }
                else
                {
                    Hide();
                }
            }
        }
    }
}
