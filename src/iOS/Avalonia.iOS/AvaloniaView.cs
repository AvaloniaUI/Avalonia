#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using Avalonia.Controls;
using Avalonia.Controls.Embedding;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Input.Raw;
using Avalonia.Input.TextInput;
using Avalonia.iOS.Storage;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Rendering.Composition;
using CoreAnimation;
using Foundation;
using ObjCRuntime;
using OpenGLES;
using UIKit;
using IInsetsManager = Avalonia.Controls.Platform.IInsetsManager;

namespace Avalonia.iOS
{
    public partial class AvaloniaView : UIView, ITextInputMethodImpl
    {
        internal IInputRoot InputRoot
            => _inputRoot ?? throw new InvalidOperationException($"{nameof(IWindowImpl.SetInputRoot)} must have been called");

        private readonly TopLevelImpl _topLevelImpl;
        private readonly EmbeddableControlRoot _topLevel;
        private readonly TouchHandler _touches;
        private TextInputMethodClient? _client;
        private IAvaloniaViewController? _controller;
        private IInputRoot? _inputRoot;

        public AvaloniaView()
        {
            _topLevelImpl = new TopLevelImpl(this);
            _touches = new TouchHandler(this, _topLevelImpl);
            _topLevel = new EmbeddableControlRoot(_topLevelImpl);

            _topLevel.Prepare();

            _topLevel.StartRendering();

            InitEagl();
            MultipleTouchEnabled = true;
        }

        [ObsoletedOSPlatform("ios12.0", "Use 'Metal' instead.")]
        [SupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("maccatalyst")]
        private void InitEagl()
        {
            var l = (CAEAGLLayer)Layer;
            l.ContentsScale = UIScreen.MainScreen.Scale;
            l.Opaque = true;
            l.DrawableProperties = new NSDictionary(
                EAGLDrawableProperty.RetainedBacking, false,
                EAGLDrawableProperty.ColorFormat, EAGLColorFormat.RGBA8
            );
            _topLevelImpl.Surfaces = new[] { new EaglLayerSurface(l) };
        }

        /// <inheritdoc />
        public override bool CanBecomeFirstResponder => true;

        /// <inheritdoc />
        public override bool CanResignFirstResponder => true;

        /// <inheritdoc />
        public override void TraitCollectionDidChange(UITraitCollection? previousTraitCollection)
        {
            base.TraitCollectionDidChange(previousTraitCollection);
            
            var settings = AvaloniaLocator.Current.GetRequiredService<IPlatformSettings>() as PlatformSettings;
            settings?.TraitCollectionDidChange();
        }

        /// <inheritdoc />
        public override void TintColorDidChange()
        {
            base.TintColorDidChange();
            
            var settings = AvaloniaLocator.Current.GetRequiredService<IPlatformSettings>() as PlatformSettings;
            settings?.TraitCollectionDidChange();
        }

        public void InitWithController<TController>(TController controller)
            where TController : UIViewController, IAvaloniaViewController
        {
            _controller = controller;
            _topLevelImpl._insetsManager.InitWithController(controller);
        }
        
        internal class TopLevelImpl : ITopLevelImpl
        {
            private readonly AvaloniaView _view;
            private readonly INativeControlHostImpl _nativeControlHost;
            private readonly IStorageProvider _storageProvider;
            internal readonly InsetsManager _insetsManager;
            private readonly ClipboardImpl _clipboard;
            private IDisposable? _paddingInsets;

            public AvaloniaView View => _view;

            public TopLevelImpl(AvaloniaView view)
            {
                _view = view;
                _nativeControlHost = new NativeControlHostImpl(view);
                _storageProvider = new IOSStorageProvider(view);
                _insetsManager = new InsetsManager(view);
                _insetsManager.DisplayEdgeToEdgeChanged += (_, edgeToEdge) =>
                {
                    // iOS doesn't add any paddings/margins to the application by itself.
                    // Application is fully responsible for safe area paddings.
                    // So, unlikely to android, we need to "fake" safe area insets when edge to edge is disabled.
                    _paddingInsets?.Dispose();
                    if (!edgeToEdge && view._controller is { } controller)
                    {
                        _paddingInsets = view._topLevel.SetValue(
                            TemplatedControl.PaddingProperty,
                            controller.SafeAreaPadding,
                            BindingPriority.Style); // lower priority, so it can be redefined by user
                    }
                };
                _clipboard = new ClipboardImpl();
            }

            public void Dispose()
            {
                // No-op
            }

            public Compositor Compositor => Platform.Compositor;

            public void Invalidate(Rect rect)
            {
                // No-op
            }

            public void SetInputRoot(IInputRoot inputRoot)
            {
                _view._inputRoot = inputRoot;
            }

            public Point PointToClient(PixelPoint point) => new Point(point.X, point.Y);

            public PixelPoint PointToScreen(Point point) => new PixelPoint((int)point.X, (int)point.Y);

            public void SetCursor(ICursorImpl? cursor)
            {
                // no-op
            }

            public IPopupImpl? CreatePopup()
            {
                // In-window popups
                return null;
            }

            public void SetTransparencyLevelHint(IReadOnlyList<WindowTransparencyLevel> transparencyLevel)
            {
                // No-op
            }

            public Size ClientSize => new Size(_view.Bounds.Width, _view.Bounds.Height);
            public Size? FrameSize => null;
            public double RenderScaling => _view.ContentScaleFactor;
            public IEnumerable<object> Surfaces { get; set; } = Array.Empty<object>();
            public Action<RawInputEventArgs>? Input { get; set; }
            public Action<Rect>? Paint { get; set; }
            public Action<Size, WindowResizeReason>? Resized { get; set; }
            public Action<double>? ScalingChanged { get; set; }
            public Action<WindowTransparencyLevel>? TransparencyLevelChanged { get; set; }
            public Action? Closed { get; set; }

            public Action? LostFocus { get; set; }

            public WindowTransparencyLevel TransparencyLevel => WindowTransparencyLevel.None;

            public void SetFrameThemeVariant(PlatformThemeVariant themeVariant)
            {
                // TODO adjust status bar depending on full screen mode.
                if (OperatingSystem.IsIOSVersionAtLeast(13) && _view._controller is not null)
                {
                    _view._controller.PreferredStatusBarStyle = themeVariant switch
                    {
                        PlatformThemeVariant.Light => UIStatusBarStyle.DarkContent,
                        PlatformThemeVariant.Dark => UIStatusBarStyle.LightContent,
                        _ => UIStatusBarStyle.Default
                    };
                }
            }
            
            public AcrylicPlatformCompensationLevels AcrylicCompensationLevels { get; } =
                new AcrylicPlatformCompensationLevels();

            public object? TryGetFeature(Type featureType)
            {
                if (featureType == typeof(IStorageProvider))
                {
                    return _storageProvider;
                }

                if (featureType == typeof(ITextInputMethodImpl))
                {
                    return _view;
                }

                if (featureType == typeof(INativeControlHostImpl))
                {
                    return _nativeControlHost;
                }

                if (featureType == typeof(IInsetsManager))
                {
                    return _insetsManager;
                }

                if (featureType == typeof(IClipboard))
                {
                    return _clipboard;
                }

                if (featureType == typeof(IInputPane))
                {
                    return UIKitInputPane.Instance;
                }

                return null;
            }
        }

        [Export("layerClass")]
        public static Class LayerClass()
        {
            return new Class(typeof(CAEAGLLayer));
        }

        public override void TouchesBegan(NSSet touches, UIEvent? evt) => _touches.Handle(touches, evt);

        public override void TouchesMoved(NSSet touches, UIEvent? evt) => _touches.Handle(touches, evt);

        public override void TouchesEnded(NSSet touches, UIEvent? evt) => _touches.Handle(touches, evt);

        public override void TouchesCancelled(NSSet touches, UIEvent? evt) => _touches.Handle(touches, evt);

        public override void LayoutSubviews()
        {
            _topLevelImpl.Resized?.Invoke(_topLevelImpl.ClientSize, WindowResizeReason.Layout);
            base.LayoutSubviews();
        }

        public Control? Content
        {
            get => (Control?)_topLevel.Content;
            set => _topLevel.Content = value;
        }
    }
}
