using System;
using System.Collections.Generic;
using System.Threading;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Runtime;
using Android.Text;
using Android.Views;
using Android.Views.InputMethods;
using AndroidX.AppCompat.App;
using Avalonia.Android.Platform.Input;
using Avalonia.Android.Platform.Specific;
using Avalonia.Android.Platform.Specific.Helpers;
using Avalonia.Android.Platform.Storage;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Input.Raw;
using Avalonia.Input.TextInput;
using Avalonia.OpenGL.Egl;
using Avalonia.OpenGL.Surfaces;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Rendering.Composition;
using Java.Lang;
using ClipboardManager = Android.Content.ClipboardManager;

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
        private readonly AndroidSystemNavigationManagerImpl _systemNavigationManager;
        private readonly AndroidInsetsManager _insetsManager;
        private readonly ClipboardImpl _clipboard;
        private readonly AndroidLauncher _launcher;
        private ViewImpl _view;
        private WindowTransparencyLevel _transparencyLevel;

        public TopLevelImpl(AvaloniaView avaloniaView, bool placeOnTop = false)
        {
            _view = new ViewImpl(avaloniaView.Context, this, placeOnTop);
            _textInputMethod = new AndroidInputMethod<ViewImpl>(_view);
            _keyboardHelper = new AndroidKeyboardEventsHelper<TopLevelImpl>(this);
            _pointerHelper = new AndroidMotionEventsHelper(this);
            _gl = new EglGlPlatformSurface(this);
            _framebuffer = new FramebufferManager(this);
            _clipboard = new ClipboardImpl(avaloniaView.Context?.GetSystemService(Context.ClipboardService).JavaCast<ClipboardManager>());

            RenderScaling = _view.Scaling;

            MaxClientSize = new PixelSize(_view.Resources.DisplayMetrics.WidthPixels,
                _view.Resources.DisplayMetrics.HeightPixels).ToSize(RenderScaling);

            if (avaloniaView.Context is AvaloniaMainActivity mainActivity)
            {
                _insetsManager = new AndroidInsetsManager(mainActivity, this);
            }

            _nativeControlHost = new AndroidNativeControlHostImpl(avaloniaView);
            _storageProvider = new AndroidStorageProvider((Activity)avaloniaView.Context);
            _transparencyLevel = WindowTransparencyLevel.None;
            _launcher = new AndroidLauncher((Activity)avaloniaView.Context);

            _systemNavigationManager = new AndroidSystemNavigationManagerImpl(avaloniaView.Context as IActivityNavigationService);

            Surfaces = new object[] { _gl, _framebuffer, Handle };
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

        public Action<Size, WindowResizeReason> Resized { get; set; }

        public Action<double> ScalingChanged { get; set; }

        public View View => _view;

        internal InvalidationAwareSurfaceView InternalView => _view;

        public IPlatformHandle Handle => _view;

        public IEnumerable<object> Surfaces { get; }

        public Compositor Compositor => AndroidPlatform.Compositor;
        
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
            _systemNavigationManager.Dispose();
            _view.Dispose();
            _view = null;
        }

        protected virtual void OnResized(Size size)
        {
            Resized?.Invoke(size, WindowResizeReason.Unspecified);
        }

        internal void Resize(Size size)
        {
            Resized?.Invoke(size, WindowResizeReason.Layout);
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

        public WindowTransparencyLevel TransparencyLevel 
        {
            get => _transparencyLevel;
            private set
            {
                if (_transparencyLevel != value)
                {
                    _transparencyLevel = value;
                    TransparencyLevelChanged?.Invoke(value);
                }
            }
        }

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

            AppCompatDelegate.DefaultNightMode = themeVariant == PlatformThemeVariant.Light ? AppCompatDelegate.ModeNightNo : AppCompatDelegate.ModeNightYes;
        }

        public AcrylicPlatformCompensationLevels AcrylicCompensationLevels => new AcrylicPlatformCompensationLevels(1, 1, 1);

        IntPtr EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo.Handle => ((IPlatformHandle)_view).Handle;

        public PixelSize Size => _view.Size;

        public double Scaling => RenderScaling;

        public void SetTransparencyLevelHint(IReadOnlyList<WindowTransparencyLevel> transparencyLevels)
        {
            if (_view.Context is not AvaloniaMainActivity activity)
                return;

            foreach (var level in transparencyLevels)
            {
                if (!IsSupported(level))
                {
                    continue;
                }

                if (level == TransparencyLevel)
                {
                    return;
                }

                if (level == WindowTransparencyLevel.None)
                {
                    if (OperatingSystem.IsAndroidVersionAtLeast(30))
                    {
                        activity.SetTranslucent(false);
                    }

                    activity.Window?.SetBackgroundDrawable(new ColorDrawable(Color.White));
                }
                else if (level == WindowTransparencyLevel.Transparent)
                {
                    if (OperatingSystem.IsAndroidVersionAtLeast(30))
                    {
                        activity.SetTranslucent(true);
                        SetBlurBehind(activity, 0);
                        activity.Window?.SetBackgroundDrawable(new ColorDrawable(Color.Transparent));
                    }
                }
                else if (level == WindowTransparencyLevel.Blur)
                {
                    if (OperatingSystem.IsAndroidVersionAtLeast(31))
                    {
                        activity.SetTranslucent(true);
                        SetBlurBehind(activity, 120);
                        activity.Window?.SetBackgroundDrawable(new ColorDrawable(Color.Transparent));
                    }
                }

                TransparencyLevel = level;
                return;
            }

            // If we get here, we didn't find a supported level. Use the default of None.
            if (OperatingSystem.IsAndroidVersionAtLeast(30))
            {
                activity.SetTranslucent(false);
            }

            activity.Window?.SetBackgroundDrawable(new ColorDrawable(Color.White));
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

            if (featureType == typeof(IInsetsManager) || featureType == typeof(IInputPane))
            {
                return _insetsManager;
            }

            if(featureType == typeof(IClipboard))
            {
                return _clipboard;
            }

            if (featureType == typeof(ILauncher))
            {
                return _launcher;
            }

            return null;
        }

        private static bool IsSupported(WindowTransparencyLevel level)
        {
            if (level == WindowTransparencyLevel.None)
                return true;
            if (level == WindowTransparencyLevel.Transparent)
                return OperatingSystem.IsAndroidVersionAtLeast(30);
            if (level == WindowTransparencyLevel.Blur)
                return OperatingSystem.IsAndroidVersionAtLeast(31);
            return false;
        }

        private static void SetBlurBehind(AvaloniaMainActivity activity, int radius)
        {
            if (radius == 0)
                activity.Window?.ClearFlags(WindowManagerFlags.BlurBehind);
            else
                activity.Window?.AddFlags(WindowManagerFlags.BlurBehind);

            if (OperatingSystem.IsAndroidVersionAtLeast(31) && activity.Window?.Attributes is { } attr)
            {
                attr.BlurBehindRadius = radius;
                activity.Window.Attributes = attr;
            }
        }

        internal void TextInput(string text)
        {
            if(Input != null)
            {
                var args = new RawTextInputEventArgs(AndroidKeyboardDevice.Instance, (ulong)DateTime.Now.Ticks, InputRoot, text);

                Input(args);
            }
        }
    }

    internal class EditableWrapper : SpannableStringBuilder
    {
        private readonly AvaloniaInputConnection _inputConnection;

        public EditableWrapper(AvaloniaInputConnection inputConnection)
        {
            _inputConnection = inputConnection;
        }

        public TextSelection CurrentSelection => new TextSelection(Selection.GetSelectionStart(this), Selection.GetSelectionEnd(this));
        public TextSelection CurrentComposition => new TextSelection(BaseInputConnection.GetComposingSpanStart(this), BaseInputConnection.GetComposingSpanEnd(this));

        public bool IgnoreChange { get; set; }

        public override IEditable Replace(int start, int end, ICharSequence tb)
        {
            if (!IgnoreChange && start != end)
            {
                SelectSurroundingTextForDeletion(start, end);
            }

            return base.Replace(start, end, tb);
        }

        public override IEditable Replace(int start, int end, ICharSequence tb, int tbstart, int tbend)
        {
            if (!IgnoreChange && start != end)
            {
                SelectSurroundingTextForDeletion(start, end);
            }

            return base.Replace(start, end, tb, tbstart, tbend);
        }

        private void SelectSurroundingTextForDeletion(int start, int end)
        {
            _inputConnection.InputMethod.Client.Selection = new TextSelection(start, end);
        }
    }

    internal class AvaloniaInputConnection : BaseInputConnection
    {
        private readonly TopLevelImpl _toplevel;
        private readonly IAndroidInputMethod _inputMethod;
        private readonly EditableWrapper _editable;
        private bool _commitInProgress;
        private int _batchLevel = 0;

        public AvaloniaInputConnection(TopLevelImpl toplevel, IAndroidInputMethod inputMethod) : base(inputMethod.View, true)
        {
            _toplevel = toplevel;
            _inputMethod = inputMethod;
            _editable = new EditableWrapper(this);
        }

        public int ExtractedTextToken { get; private set; }

        public override IEditable Editable => _editable;

        public EditableWrapper EditableWrapper => _editable;

        public IAndroidInputMethod InputMethod => _inputMethod;

        public TopLevelImpl Toplevel => _toplevel;

        public bool IsInBatchEdit => _batchLevel > 0;

        public override bool SetComposingRegion(int start, int end)
        {
            return base.SetComposingRegion(start, end);
        }

        public override bool SetComposingText(ICharSequence text, int newCursorPosition)
        {
            BeginBatchEdit();
            _editable.IgnoreChange = true;

            try
            {
                if (_editable.CurrentComposition.Start > -1)
                {
                    // Select the composing region.
                    InputMethod.Client.Selection = new TextSelection(_editable.CurrentComposition.Start, _editable.CurrentComposition.End);
                }
                var compositionText = text.SubSequence(0, text.Length());

                if (_inputMethod.IsActive && !_commitInProgress)
                {
                    if (string.IsNullOrEmpty(compositionText))
                        _inputMethod.View.DispatchKeyEvent(new KeyEvent(KeyEventActions.Down, Keycode.ForwardDel));

                    else
                        _toplevel.TextInput(compositionText);
                }
                base.SetComposingText(text, newCursorPosition);
            }
            finally
            {
                _editable.IgnoreChange = false;

                EndBatchEdit();
            }

            return true;
        }

        public override bool BeginBatchEdit()
        {
            _batchLevel = Interlocked.Increment(ref _batchLevel);
            return base.BeginBatchEdit();
        }

        public override bool EndBatchEdit()
        {
            _batchLevel = Interlocked.Decrement(ref _batchLevel);

            _inputMethod.OnBatchEditedEnded();
            return base.EndBatchEdit();
        }

        public override bool CommitText(ICharSequence text, int newCursorPosition)
        {
            BeginBatchEdit();
            _commitInProgress = true;

            var composingRegion = _editable.CurrentComposition;

            var ret = base.CommitText(text, newCursorPosition);

            if(composingRegion.Start != -1)
            {
                InputMethod.Client.Selection = composingRegion;
            }

            var committedText = text.SubSequence(0, text.Length());

            if (_inputMethod.IsActive)
                if (string.IsNullOrEmpty(committedText))
                    _inputMethod.View.DispatchKeyEvent(new KeyEvent(KeyEventActions.Down, Keycode.ForwardDel));
                else
                    _toplevel.TextInput(committedText);

            _commitInProgress = false;
            EndBatchEdit();

            return true;
        }

        public override bool DeleteSurroundingText(int beforeLength, int afterLength)
        {
            if (InputMethod.IsActive)
            {
                EditableWrapper.IgnoreChange = true;
            }

            if (InputMethod.IsActive)
            {
                var selection = _editable.CurrentSelection;

                InputMethod.Client.Selection = new TextSelection(selection.Start - beforeLength, selection.End + afterLength);

                InputMethod.View.DispatchKeyEvent(new KeyEvent(KeyEventActions.Down, Keycode.ForwardDel));

                EditableWrapper.IgnoreChange = true;
            }

            return true;
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
                case ImeAction.Next:
                    {
                        FocusManager.GetFocusManager(_toplevel.InputRoot)?
                            .TryMoveFocus(NavigationDirection.Next);
                        break;
                    }
            }

            return base.PerformEditorAction(actionCode);
        }

        public override ExtractedText GetExtractedText(ExtractedTextRequest request, [GeneratedEnum] GetTextFlags flags)
        {
            if (request == null)
                return null;

            ExtractedTextToken = request.Token;

            var editable = Editable;

            if (editable == null)
            {
                return null;
            }

            if (!_inputMethod.IsActive)
            {
                return null;
            }

            var selection = _editable.CurrentSelection;

            ExtractedText extract = new ExtractedText
            {
                Flags = 0,
                PartialStartOffset = -1,
                PartialEndOffset = -1,
                SelectionStart = selection.Start,
                SelectionEnd = selection.End,
                StartOffset = 0
            };

            if ((request.Flags & GetTextFlags.WithStyles) != 0)
            {
                extract.Text = new SpannableString(editable);
            }
            else
            {
                extract.Text = editable;
            }

            return extract;
        }
    }
}
