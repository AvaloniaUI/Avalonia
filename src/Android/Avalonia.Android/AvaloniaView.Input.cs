using System;
using Android.Views;
using Android.Views.InputMethods;
using Avalonia.Android.Platform.SkiaPlatform;

namespace Avalonia.Android
{
    public partial class AvaloniaView : IInitEditorInfo
    {
        private Func<TopLevelImpl, EditorInfo, IInputConnection>? _initEditorInfo;

        public override IInputConnection OnCreateInputConnection(EditorInfo? outAttrs)
        {
            return _initEditorInfo?.Invoke(_view, outAttrs!)!;
        }

        void IInitEditorInfo.InitEditorInfo(Func<TopLevelImpl, EditorInfo, IInputConnection> init)
        {
            _initEditorInfo = init;
        }

        protected override void OnFocusChanged(bool gainFocus, FocusSearchDirection direction, global::Android.Graphics.Rect? previouslyFocusedRect)
        {
            base.OnFocusChanged(gainFocus, direction, previouslyFocusedRect);
            _accessHelper.OnFocusChanged(gainFocus, (int)direction, previouslyFocusedRect);
        }

        protected override bool DispatchHoverEvent(MotionEvent? e)
        {
            return _accessHelper.DispatchHoverEvent(e!) || base.DispatchHoverEvent(e);
        }

        protected override bool DispatchGenericPointerEvent(MotionEvent? e)
        {
            var result = _view.PointerHelper.DispatchMotionEvent(e, out var callBase);

            var baseResult = callBase && base.DispatchGenericPointerEvent(e);

            return result ?? baseResult;
        }

        public override bool DispatchTouchEvent(MotionEvent? e)
        {
            var result = _view.PointerHelper.DispatchMotionEvent(e, out var callBase);
            var baseResult = callBase && base.DispatchTouchEvent(e);

            if(result == true)
            {
                // Request focus for this view
                RequestFocus();
            }

            return result ?? baseResult;
        }

        public override bool DispatchKeyEvent(KeyEvent? e)
        {
            var res = _view.KeyboardHelper.DispatchKeyEvent(e, out var callBase);
            if (res == false)
                callBase = !_accessHelper.DispatchKeyEvent(e!) && callBase;

            var baseResult = callBase && base.DispatchKeyEvent(e);

            return res ?? baseResult;
        }
    }
}
