using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Embedding;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Input.TextInput;
using Avalonia.iOS.Storage;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using CoreAnimation;
using Foundation;
using ObjCRuntime;
using OpenGLES;
using UIKit;

namespace Avalonia.iOS
{
    public partial class AvaloniaView : UIView, ITextInputMethodImpl
    {
        internal IInputRoot InputRoot { get; private set; }
        private TopLevelImpl _topLevelImpl;
        private EmbeddableControlRoot _topLevel;
        private TouchHandler _touches;
        private ITextInputMethodClient _client;
        private bool _isActive;

        public AvaloniaView()
        {
            _topLevelImpl = new TopLevelImpl(this);
            _touches = new TouchHandler(this, _topLevelImpl);
            _topLevel = new EmbeddableControlRoot(_topLevelImpl);

            _topLevel.Prepare();

            _topLevel.Renderer.Start();

            var l = (CAEAGLLayer)Layer;
            l.ContentsScale = UIScreen.MainScreen.Scale;
            l.Opaque = true;
            l.DrawableProperties = new NSDictionary(
                EAGLDrawableProperty.RetainedBacking, false,
                EAGLDrawableProperty.ColorFormat, EAGLColorFormat.RGBA8
            );
            _topLevelImpl.Surfaces = new[] { new EaglLayerSurface(l) };
            MultipleTouchEnabled = true;
            AddSubviews(new UIView[] { new UIKit.UIButton(UIButtonType.InfoDark) });
        }

        public override bool CanBecomeFirstResponder => true;

        public override bool CanResignFirstResponder => true;

        internal class TopLevelImpl : ITopLevelImplWithTextInputMethod, ITopLevelImplWithNativeControlHost,
            ITopLevelImplWithStorageProvider
        {
            private readonly AvaloniaView _view;
            public AvaloniaView View => _view;

            public TopLevelImpl(AvaloniaView view)
            {
                _view = view;
                NativeControlHost = new NativeControlHostImpl(_view);
                StorageProvider = new IOSStorageProvider(view);
            }

            public void Dispose()
            {
                // No-op
            }

            public IRenderer CreateRenderer(IRenderRoot root) => new CompositingRenderer(root, Platform.Compositor);


            public void Invalidate(Rect rect)
            {
                // No-op
            }

            public void SetInputRoot(IInputRoot inputRoot)
            {
                _view.InputRoot = inputRoot;
            }

            public Point PointToClient(PixelPoint point) => new Point(point.X, point.Y);

            public PixelPoint PointToScreen(Point point) => new PixelPoint((int)point.X, (int)point.Y);

            public void SetCursor(ICursorImpl _)
            {
                // no-op
            }

            public IPopupImpl CreatePopup()
            {
                // In-window popups
                return null;
            }

            public void SetTransparencyLevelHint(WindowTransparencyLevel transparencyLevel)
            {
                // No-op
            }

            public Size ClientSize => new Size(_view.Bounds.Width, _view.Bounds.Height);
            public Size? FrameSize => null;
            public double RenderScaling => _view.ContentScaleFactor;
            public IEnumerable<object> Surfaces { get; set; }
            public Action<RawInputEventArgs> Input { get; set; }
            public Action<Rect> Paint { get; set; }
            public Action<Size, PlatformResizeReason> Resized { get; set; }
            public Action<double> ScalingChanged { get; set; }
            public Action<WindowTransparencyLevel> TransparencyLevelChanged { get; set; }
            public Action Closed { get; set; }

            public Action LostFocus { get; set; }

            // legacy no-op
            public IMouseDevice MouseDevice { get; } = new MouseDevice();
            public WindowTransparencyLevel TransparencyLevel { get; }

            public AcrylicPlatformCompensationLevels AcrylicCompensationLevels { get; } =
                new AcrylicPlatformCompensationLevels();

            public ITextInputMethodImpl? TextInputMethod => _view;
            public INativeControlHostImpl NativeControlHost { get; }
            public IStorageProvider StorageProvider { get; }
        }

        [Export("layerClass")]
        public static Class LayerClass()
        {
            return new Class(typeof(CAEAGLLayer));
        }

        public override void TouchesBegan(NSSet touches, UIEvent evt) => _touches.Handle(touches, evt);

        public override void TouchesMoved(NSSet touches, UIEvent evt) => _touches.Handle(touches, evt);

        public override void TouchesEnded(NSSet touches, UIEvent evt) => _touches.Handle(touches, evt);

        public override void TouchesCancelled(NSSet touches, UIEvent evt) => _touches.Handle(touches, evt);

        public override void LayoutSubviews()
        {
            _topLevelImpl.Resized?.Invoke(_topLevelImpl.ClientSize, PlatformResizeReason.Layout);
            base.LayoutSubviews();
        }

        public Control Content
        {
            get => (Control)_topLevel.Content;
            set => _topLevel.Content = value;
        }

        ITextInputMethodClient ITextInputMethodImpl.Client => _client;

        bool ITextInputMethodImpl.IsActive => _isActive;

        private AvaloniaResponder _currentResponder;

        void ITextInputMethodImpl.SetClient(ITextInputMethodClient client)
        {
            if (_client != null)
            {
                //_client.CursorRectangleChanged -= ClientOnCursorRectangleChanged;
            }

            _client = client;

            if (_client is { })
            {
                if (_currentResponder != null && _currentResponder.IsFirstResponder)
                {
                    _currentResponder.ResignFirstResponder();
                    _currentResponder = null;
                }

                _currentResponder = new AvaloniaResponder(this, _client);
                //_client.CursorRectangleChanged += ClientOnCursorRectangleChanged;

                //_client.SurroundingTextChanged += ClientOnSurroundingTextChanged;
                _currentResponder.BecomeFirstResponder();

                var x = _currentResponder.IsFirstResponder;
            }
            else
            {
                _currentResponder.ResignFirstResponder();
            }
        }

        void ITextInputMethodImpl.SetCursorRect(Rect rect)
        {
            // maybe this will be cursor / selection rect?
        }

        void ITextInputMethodImpl.SetOptions(TextInputOptions options)
        {
            /*IsSecureEntry = false;

            switch (options.ContentType)
            {
                case TextInputContentType.Normal:
                    KeyboardType = UIKeyboardType.Default;
                    break;

                case TextInputContentType.Alpha:
                    KeyboardType = UIKeyboardType.AsciiCapable;
                    break;

                case TextInputContentType.Digits:
                    KeyboardType = UIKeyboardType.PhonePad;
                    break;

                case TextInputContentType.Pin:
                    KeyboardType = UIKeyboardType.NumberPad;
                    IsSecureEntry = true;
                    break;

                case TextInputContentType.Number:
                    KeyboardType = UIKeyboardType.PhonePad;
                    break;

                case TextInputContentType.Email:
                    KeyboardType = UIKeyboardType.EmailAddress;
                    break;

                case TextInputContentType.Url:
                    KeyboardType = UIKeyboardType.Url;
                    break;

                case TextInputContentType.Name:
                    KeyboardType = UIKeyboardType.NamePhonePad;
                    break;

                case TextInputContentType.Password:
                    KeyboardType = UIKeyboardType.Default;
                    IsSecureEntry = true;
                    break;

                case TextInputContentType.Social:
                    KeyboardType = UIKeyboardType.Twitter;
                    break;

                case TextInputContentType.Search:
                    KeyboardType = UIKeyboardType.WebSearch;
                    break;
            }

            if (options.IsSensitive)
            {
                IsSecureEntry = true;
            }

            ReturnKeyType = (UIReturnKeyType)options.ReturnKeyType;
            AutocorrectionType = UITextAutocorrectionType.Yes;
            SpellCheckingType = UITextSpellCheckingType.Yes;
            KeyboardAppearance = UIKeyboardAppearance.Alert;*/
        }


        void ITextInputMethodImpl.Reset()
        {
            _currentResponder?.ResignFirstResponder();
        }
    }
}
