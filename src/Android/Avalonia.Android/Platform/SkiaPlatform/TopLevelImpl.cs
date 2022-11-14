using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Text;
using Android.Views;
using Android.Views.InputMethods;
using Avalonia.Android.OpenGL;
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
using Math = System.Math;

namespace Avalonia.Android.Platform.SkiaPlatform
{
    class TopLevelImpl : IAndroidView, ITopLevelImpl, EglGlPlatformSurfaceBase.IEglWindowGlPlatformSurfaceInfo,
        ITopLevelImplWithTextInputMethod, ITopLevelImplWithNativeControlHost, ITopLevelImplWithStorageProvider
    {
        private readonly IGlPlatformSurface _gl;
        private readonly IFramebufferPlatformSurface _framebuffer;

        private readonly AndroidKeyboardEventsHelper<TopLevelImpl> _keyboardHelper;
        private readonly AndroidMotionEventsHelper _pointerHelper;
        private readonly AndroidInputMethod<ViewImpl> _textInputMethod;
        private ViewImpl _view;

        public TopLevelImpl(AvaloniaView avaloniaView, bool placeOnTop = false)
        {
            _view = new ViewImpl(avaloniaView.Context, this, placeOnTop);
            _textInputMethod = new AndroidInputMethod<ViewImpl>(_view);
            _keyboardHelper = new AndroidKeyboardEventsHelper<TopLevelImpl>(this);
            _pointerHelper = new AndroidMotionEventsHelper(this);
            _gl = GlPlatformSurface.TryCreate(this);
            _framebuffer = new FramebufferManager(this);

            RenderScaling = _view.Scaling;

            MaxClientSize = new PixelSize(_view.Resources.DisplayMetrics.WidthPixels,
                _view.Resources.DisplayMetrics.HeightPixels).ToSize(RenderScaling);

            NativeControlHost = new AndroidNativeControlHostImpl(avaloniaView);
            StorageProvider = new AndroidStorageProvider((Activity)avaloniaView.Context);
        }

        public virtual Point GetAvaloniaPointFromEvent(MotionEvent e, int pointerIndex) =>
            new Point(e.GetX(pointerIndex), e.GetY(pointerIndex)) / RenderScaling;

        public IInputRoot InputRoot { get; private set; }

        public virtual Size ClientSize => Size.ToSize(RenderScaling);

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
            AndroidPlatform.Options.UseCompositor
                ? new CompositingRenderer(root, AndroidPlatform.Compositor)
                : AndroidPlatform.Options.UseDeferredRendering
                    ? new DeferredRenderer(root, AvaloniaLocator.Current.GetRequiredService<IRenderLoop>()) { RenderOnlyOnRenderThread = true }
                    : new ImmediateRenderer(root);

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

        public WindowTransparencyLevel TransparencyLevel => WindowTransparencyLevel.None;

        public AcrylicPlatformCompensationLevels AcrylicCompensationLevels => new AcrylicPlatformCompensationLevels(1, 1, 1);

        IntPtr EglGlPlatformSurfaceBase.IEglWindowGlPlatformSurfaceInfo.Handle => ((IPlatformHandle)_view).Handle;

        public PixelSize Size => _view.Size;

        public double Scaling => RenderScaling;

        public ITextInputMethodImpl TextInputMethod => _textInputMethod;

        public INativeControlHostImpl NativeControlHost { get; }
        
        public IStorageProvider StorageProvider { get; }

        public void SetTransparencyLevelHint(WindowTransparencyLevel transparencyLevel)
        {
            throw new NotImplementedException();
        }
    }

    internal class AvaloniaInputConnection : BaseInputConnection
    {
        private readonly TopLevelImpl _topLevel;
        private readonly IAndroidInputMethod _inputMethod;

        public AvaloniaInputConnection(TopLevelImpl topLevel, IAndroidInputMethod inputMethod) : base(inputMethod.View, true)
        {
            _topLevel = topLevel;
            _inputMethod = inputMethod;
        }

        public TextInputMethodSurroundingText SurroundingText { get; set; }

        public string ComposingText { get; internal set; }

        public ComposingRegion? ComposingRegion { get; internal set; }

        public bool IsComposing => !string.IsNullOrEmpty(ComposingText);
        public bool IsCommiting { get; private set; }

        public override bool SetComposingRegion(int start, int end)
        {
            //System.Diagnostics.Debug.WriteLine($"Composing Region: [{start}|{end}] {SurroundingText.Text?.Substring(start, end - start)}");

            ComposingRegion = new ComposingRegion(start, end);

            return base.SetComposingRegion(start, end);
        }

        public override bool SetComposingText(ICharSequence text, int newCursorPosition)
        {
            var composingText = text.ToString();

            ComposingText = composingText;

            _inputMethod.Client?.SetPreeditText(ComposingText);

            return base.SetComposingText(text, newCursorPosition);
        }

        public override bool FinishComposingText()
        {
            if (!string.IsNullOrEmpty(ComposingText))
            {
                CommitText(ComposingText, ComposingText.Length);
            }
            else
            {
                ComposingRegion = new ComposingRegion(SurroundingText.CursorOffset, SurroundingText.CursorOffset);
            }

            return base.FinishComposingText();
        }

        public override ICharSequence GetTextBeforeCursorFormatted(int length, [GeneratedEnum] GetTextFlags flags)
        {
            if (!string.IsNullOrEmpty(SurroundingText.Text) && length > 0)
            {
                var start = System.Math.Max(SurroundingText.CursorOffset - length, 0);

                var end = System.Math.Min(start + length - 1, SurroundingText.CursorOffset);

                var text = SurroundingText.Text.Substring(start, end - start);

                //System.Diagnostics.Debug.WriteLine($"Text Before: {text}");

                return new Java.Lang.String(text);
            }

            return null;
        }

        public override ICharSequence GetTextAfterCursorFormatted(int length, [GeneratedEnum] GetTextFlags flags)
        {
            if (!string.IsNullOrEmpty(SurroundingText.Text))
            {
                var start = SurroundingText.CursorOffset;

                var end = System.Math.Min(start + length, SurroundingText.Text.Length);

                var text = SurroundingText.Text.Substring(start, end - start);

                //System.Diagnostics.Debug.WriteLine($"Text After: {text}");

                return new Java.Lang.String(text);
            }

            return null;
        }

        public override bool CommitText(ICharSequence text, int newCursorPosition)
        {
            IsCommiting = true;
            var committedText = text.ToString();

            _inputMethod.Client.SetPreeditText(null);

            int? start, end;

            if(SurroundingText.CursorOffset != SurroundingText.AnchorOffset)
            {
                start = Math.Min(SurroundingText.CursorOffset, SurroundingText.AnchorOffset);
                end = Math.Max(SurroundingText.CursorOffset, SurroundingText.AnchorOffset);
            }
            else if (ComposingRegion != null)
            {
                start = ComposingRegion?.Start;
                end = ComposingRegion?.End;

                ComposingRegion = null;
            }
            else
            {
                start = end = _inputMethod.Client.SurroundingText.CursorOffset;
            }

            _inputMethod.Client.SelectInSurroundingText((int)start, (int)end);

            var time = DateTime.Now.TimeOfDay;

            var rawTextEvent = new RawTextInputEventArgs(KeyboardDevice.Instance, (ulong)time.Ticks, _topLevel.InputRoot, committedText);

            _topLevel.Input(rawTextEvent);

            ComposingText = null;

            ComposingRegion = new ComposingRegion(newCursorPosition, newCursorPosition);

            return base.CommitText(text, newCursorPosition);
        }

        public override bool DeleteSurroundingText(int beforeLength, int afterLength)
        {
            var surroundingText = _inputMethod.Client.SurroundingText;

            var selectionStart = surroundingText.CursorOffset;

            _inputMethod.Client.SelectInSurroundingText(selectionStart - beforeLength, selectionStart + afterLength);

            _inputMethod.View.DispatchKeyEvent(new KeyEvent(KeyEventActions.Down, Keycode.ForwardDel));

            surroundingText = _inputMethod.Client.SurroundingText;

            selectionStart = surroundingText.CursorOffset;

            ComposingRegion = new ComposingRegion(selectionStart, selectionStart);

            return base.DeleteSurroundingText(beforeLength, afterLength);
        }

        public override bool SetSelection(int start, int end)
        {
            _inputMethod.Client.SelectInSurroundingText(start, end);

            ComposingRegion = new ComposingRegion(start, end);

            return base.SetSelection(start, end);
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
