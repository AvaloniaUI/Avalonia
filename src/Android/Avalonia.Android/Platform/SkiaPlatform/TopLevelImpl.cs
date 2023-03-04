using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Views;
using Android.Views.InputMethods;
using Avalonia.Android.Platform.Specific;
using Avalonia.Android.Platform.Specific.Helpers;
using Avalonia.Android.Platform.Storage;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Input.TextInput;
using Avalonia.OpenGL.Egl;
using Avalonia.OpenGL.Surfaces;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Java.Lang;
using Java.Util;
using Math = System.Math;
using AndroidRect = Android.Graphics.Rect;
using Window = Android.Views.Window;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Text;

namespace Avalonia.Android.Platform.SkiaPlatform
{
    class TopLevelImpl : IAndroidView, ITopLevelImpl, EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo
    {
        private readonly IGlPlatformSurface _gl;
        private readonly IFramebufferPlatformSurface _framebuffer;

        private readonly AndroidKeyboardEventsHelper<TopLevelImpl> _keyboardHelper;
        private readonly AndroidMotionEventsHelper _pointerHelper;
        private readonly AndroidInputMethod<ViewImpl> _textInputMethod;
        private readonly INativeControlHostImpl _nativeControlHost;
        private readonly IStorageProvider _storageProvider;
        private readonly ISystemNavigationManagerImpl _systemNavigationManager;
        private readonly AndroidInsetsManager _insetsManager;
        private ViewImpl _view;

        public TopLevelImpl(AvaloniaView avaloniaView, bool placeOnTop = false)
        {
            _view = new ViewImpl(avaloniaView.Context, this, placeOnTop);
            _textInputMethod = new AndroidInputMethod<ViewImpl>(_view);
            _keyboardHelper = new AndroidKeyboardEventsHelper<TopLevelImpl>(this);
            _pointerHelper = new AndroidMotionEventsHelper(this);
            _gl = new EglGlPlatformSurface(this);
            _framebuffer = new FramebufferManager(this);

            RenderScaling = _view.Scaling;

            MaxClientSize = new PixelSize(_view.Resources.DisplayMetrics.WidthPixels,
                _view.Resources.DisplayMetrics.HeightPixels).ToSize(RenderScaling);

            if (avaloniaView.Context is AvaloniaMainActivity mainActivity)
            {
                _insetsManager = new AndroidInsetsManager(mainActivity, this);
            }

            _nativeControlHost = new AndroidNativeControlHostImpl(avaloniaView);
            _storageProvider = new AndroidStorageProvider((Activity)avaloniaView.Context);

            _systemNavigationManager = new AndroidSystemNavigationManagerImpl(avaloniaView.Context as IActivityNavigationService);
        }

        public virtual Point GetAvaloniaPointFromEvent(MotionEvent e, int pointerIndex) =>
            new Point(e.GetX(pointerIndex), e.GetY(pointerIndex)) / RenderScaling;

        public IInputRoot InputRoot { get; private set; }

        public virtual Size ClientSize => _view.Size.ToSize(RenderScaling);

        public Size? FrameSize => null;

        public IMouseDevice MouseDevice { get; } = new MouseDevice();

        public Action Closed { get; set; }

        public Action<RawInputEventArgs> Input { get; set; }

        public Size MaxClientSize { get; protected set; }

        public Action<Rect> Paint { get; set; }

        public Action<Size, PlatformResizeReason> Resized { get; set; }

        public Action<double> ScalingChanged { get; set; }

        public View View => _view;

        internal InvalidationAwareSurfaceView InternalView => _view;

        public IPlatformHandle Handle => _view;

        public IEnumerable<object> Surfaces => new object[] { _gl, _framebuffer, Handle };

        public IRenderer CreateRenderer(IRenderRoot root) =>
            new CompositingRenderer(root, AndroidPlatform.Compositor, () => Surfaces);

        public virtual void Hide()
        {
            _view.Visibility = ViewStates.Invisible;
        }

        public void Invalidate(Rect rect)
        {
            if (_view.Holder?.Surface?.IsValid == true) _view.Invalidate();
        }

        public Point PointToClient(PixelPoint point)
        {
            return point.ToPoint(RenderScaling);
        }

        public PixelPoint PointToScreen(Point point)
        {
            return PixelPoint.FromPoint(point, RenderScaling);
        }

        public void SetCursor(ICursorImpl cursor)
        {
            //still not implemented
        }

        public void SetInputRoot(IInputRoot inputRoot)
        {
            InputRoot = inputRoot;
        }
        
        public virtual void Show()
        {
            _view.Visibility = ViewStates.Visible;
        }

        public double RenderScaling { get; }

        void Draw()
        {
            Paint?.Invoke(new Rect(new Point(0, 0), ClientSize));
        }
        
        public virtual void Dispose()
        {
            _view.Dispose();
            _view = null;
        }

        protected virtual void OnResized(Size size)
        {
            Resized?.Invoke(size, PlatformResizeReason.Unspecified);
        }

        internal void Resize(Size size)
        {
            Resized?.Invoke(size, PlatformResizeReason.Layout);
        }

        class ViewImpl : InvalidationAwareSurfaceView, ISurfaceHolderCallback, IInitEditorInfo
        {
            private readonly TopLevelImpl _tl;
            private Size _oldSize;

            public ViewImpl(Context context, TopLevelImpl tl, bool placeOnTop) : base(context)
            {
                _tl = tl;
                if (placeOnTop)
                    SetZOrderOnTop(true);
            }

            public TopLevelImpl TopLevelImpl => _tl;

            protected override void Draw()
            {
                _tl.Draw();
            }

            protected override void DispatchDraw(global::Android.Graphics.Canvas canvas)
            {
                // Workaround issue #9230 on where screen remains gray after splash screen.
                // base.DispatchDraw should punch a hole into the canvas so the surface
                // can be seen below, but it does not.
                if (OperatingSystem.IsAndroidVersionAtLeast(29))
                {
                    // Android 10+ does this (BlendMode was new)
                    var paint = new Paint();
                    paint.SetColor(0);
                    paint.BlendMode = BlendMode.Clear;
                    canvas.DrawRect(0, 0, Width, Height, paint);
                }
                else
                {
                    // Android 9 did this
                    canvas.DrawColor(Color.Transparent, PorterDuff.Mode.Clear);
                }

                base.DispatchDraw(canvas);
            }

            protected override bool DispatchGenericPointerEvent(MotionEvent e)
            {
                bool callBase;
                bool? result = _tl._pointerHelper.DispatchMotionEvent(e, out callBase);
                bool baseResult = callBase ? base.DispatchGenericPointerEvent(e) : false;

                return result != null ? result.Value : baseResult;
            }

            public override bool DispatchTouchEvent(MotionEvent e)
            {
                bool callBase;
                bool? result = _tl._pointerHelper.DispatchMotionEvent(e, out callBase);
                bool baseResult = callBase ? base.DispatchTouchEvent(e) : false;

                return result != null ? result.Value : baseResult;
            }

            public override bool DispatchKeyEvent(KeyEvent e)
            {
                bool callBase;
                bool? res = _tl._keyboardHelper.DispatchKeyEvent(e, out callBase);
                bool baseResult = callBase ? base.DispatchKeyEvent(e) : false;

                return res != null ? res.Value : baseResult;
            }

            void ISurfaceHolderCallback.SurfaceChanged(ISurfaceHolder holder, Format format, int width, int height)
            {
                var newSize = new PixelSize(width, height).ToSize(_tl.RenderScaling);

                if (newSize != _oldSize)
                {
                    _oldSize = newSize;
                    _tl.OnResized(newSize);
                }

                base.SurfaceChanged(holder, format, width, height);
            }

            public sealed override bool OnCheckIsTextEditor()
            {
                return true;
            }

            private Func<TopLevelImpl, EditorInfo, IInputConnection> _initEditorInfo;

            public void InitEditorInfo(Func<TopLevelImpl, EditorInfo, IInputConnection> init)
            {
                _initEditorInfo = init;
            }

            public sealed override IInputConnection OnCreateInputConnection(EditorInfo outAttrs)
            {
                if (_initEditorInfo != null)
                {
                    return _initEditorInfo(_tl, outAttrs);
                }
                   
                return null;
            }

        }

        public IPopupImpl CreatePopup() => null;
        
        public Action LostFocus { get; set; }
        public Action<WindowTransparencyLevel> TransparencyLevelChanged { get; set; }

        public WindowTransparencyLevel TransparencyLevel { get; private set; }

        public void SetFrameThemeVariant(PlatformThemeVariant themeVariant)
        {
            if(_insetsManager != null)
            {
                _insetsManager.SystemBarTheme = themeVariant switch
                {
                    PlatformThemeVariant.Light => SystemBarTheme.Light,
                    PlatformThemeVariant.Dark => SystemBarTheme.Dark,
                    _ => null,
                };
            }
        }

        public AcrylicPlatformCompensationLevels AcrylicCompensationLevels => new AcrylicPlatformCompensationLevels(1, 1, 1);

        IntPtr EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo.Handle => ((IPlatformHandle)_view).Handle;

        public PixelSize Size => _view.Size;

        public double Scaling => RenderScaling;

        public void SetTransparencyLevelHint(WindowTransparencyLevel transparencyLevel)
        {
            if (TransparencyLevel != transparencyLevel)
            {
                bool isBelowR = Build.VERSION.SdkInt < BuildVersionCodes.R;
                bool isAboveR = Build.VERSION.SdkInt > BuildVersionCodes.R;
                if (_view.Context is AvaloniaMainActivity activity)
                {
                    switch (transparencyLevel)
                    {
                        case WindowTransparencyLevel.AcrylicBlur:
                        case WindowTransparencyLevel.ForceAcrylicBlur:
                        case WindowTransparencyLevel.Mica:
                        case WindowTransparencyLevel.None:
                            if (!isBelowR)
                            {
                                activity.SetTranslucent(false);
                            }
                            if (isAboveR)
                            {
                                activity.Window?.ClearFlags(WindowManagerFlags.BlurBehind);

                                var attr = activity.Window?.Attributes;
                                if (attr != null)
                                {
                                    attr.BlurBehindRadius = 0;

                                    activity.Window.Attributes = attr;
                                }
                            }
                            activity.Window.SetBackgroundDrawable(new ColorDrawable(Color.White));

                            if(transparencyLevel != WindowTransparencyLevel.None)
                            {
                                return;
                            }
                            break;
                        case WindowTransparencyLevel.Transparent:
                            if (!isBelowR)
                            {
                                activity.SetTranslucent(true);
                            }
                            if (isAboveR)
                            {
                                activity.Window?.ClearFlags(WindowManagerFlags.BlurBehind);

                                var attr = activity.Window?.Attributes;
                                if (attr != null)
                                {
                                    attr.BlurBehindRadius = 0;

                                    activity.Window.Attributes = attr;
                                }
                            }
                            activity.Window.SetBackgroundDrawable(new ColorDrawable(Color.Transparent));
                            break;
                        case WindowTransparencyLevel.Blur:
                            if (isAboveR)
                            {
                                activity.SetTranslucent(true);
                                activity.Window?.AddFlags(WindowManagerFlags.BlurBehind);

                                var attr = activity.Window?.Attributes;
                                if (attr != null)
                                {
                                    attr.BlurBehindRadius = 120;

                                    activity.Window.Attributes = attr;
                                }
                                activity.Window.SetBackgroundDrawable(new ColorDrawable(Color.Transparent));
                            }
                            else
                            {
                                activity.Window?.ClearFlags(WindowManagerFlags.BlurBehind);
                                activity.Window.SetBackgroundDrawable(new ColorDrawable(Color.White));

                                return;
                            }
                            break;
                    }
                    TransparencyLevel = transparencyLevel;
                }
            }
        }
        
        public virtual object TryGetFeature(Type featureType)
        {
            if (featureType == typeof(IStorageProvider))
            {
                return _storageProvider;
            }

            if (featureType == typeof(ITextInputMethodImpl))
            {
                return _textInputMethod;
            }

            if (featureType == typeof(ISystemNavigationManagerImpl))
            {
                return _systemNavigationManager;
            }

            if (featureType == typeof(INativeControlHostImpl))
            {
                return _nativeControlHost;
            }

            if (featureType == typeof(IInsetsManager))
            {
                return _insetsManager;
            }

            return null;
        }
    }

    internal class AvaloniaInputConnection : BaseInputConnection
    {
        private readonly TopLevelImpl _topLevel;
        private readonly IAndroidInputMethod _inputMethod;
        private readonly InputEditable _editable;

        public AvaloniaInputConnection(TopLevelImpl topLevel, IAndroidInputMethod inputMethod) : base(inputMethod.View, true)
        {
            _topLevel = topLevel;
            _inputMethod = inputMethod;
            _editable = new InputEditable(_topLevel, _inputMethod, this);
        }

        public override IEditable Editable => _editable;

        internal InputEditable InputEditable => _editable;

        public override bool SetComposingRegion(int start, int end)
        {
            var ret = base.SetComposingRegion(start, end);

            InputEditable.RaiseCompositionChanged();

            return ret;
        }

        public override bool SetComposingText(ICharSequence text, int newCursorPosition)
        {
            var composingText = text.ToString();

            if (string.IsNullOrEmpty(composingText))
            {
                return CommitText(text, newCursorPosition);
            }
            else
            {
                var ret = base.SetComposingText(text, newCursorPosition);

                InputEditable.RaiseCompositionChanged();

                return ret;
            }
        }

        public override bool BeginBatchEdit()
        {
            _editable.BeginBatchEdit();

            return base.BeginBatchEdit();
        }

        public override bool EndBatchEdit()
        {
            var ret = base.EndBatchEdit();
            _editable.EndBatchEdit();

            return ret;
        }

        public override bool FinishComposingText()
        {
            var ret = base.FinishComposingText();
            InputEditable.RaiseCompositionChanged();
            return ret;
        }

        public override bool CommitText(ICharSequence text, int newCursorPosition)
        {
            var ret = base.CommitText(text, newCursorPosition);
            InputEditable.RaiseCompositionChanged();
            return ret;
        }

        public override bool PerformEditorAction([GeneratedEnum] ImeAction actionCode)
        {
            switch (actionCode)
            {
                case ImeAction.Done:
                    {
                        _inputMethod.IMM.HideSoftInputFromWindow(_inputMethod.View.WindowToken, HideSoftInputFlags.ImplicitOnly);
                        break;
                    }
            }

            return base.PerformEditorAction(actionCode);
        }
    }
}
