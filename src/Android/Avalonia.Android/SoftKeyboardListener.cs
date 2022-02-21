using System;
using System.Collections.Generic;
using System.Text;
using Android.Content;
using Android.OS;
using Android.Util;
using Android.Views;
using Avalonia.Input;

namespace Avalonia.Android
{
    class SoftKeyboardListener : Java.Lang.Object, ViewTreeObserver.IOnGlobalLayoutListener
    {
        private const int DefaultKeyboardHeightDP = 100;
        private static readonly int EstimatedKeyboardDP = DefaultKeyboardHeightDP + (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop ? 48 : 0);

        private readonly View _host;
        private bool _wasKeyboard;

        public SoftKeyboardListener(View view)
        {
            _host = view;
        }

        public void OnGlobalLayout()
        {
            int estimatedKeyboardHeight = (int)TypedValue.ApplyDimension(ComplexUnitType.Dip,
                EstimatedKeyboardDP, _host.Resources.DisplayMetrics);

            var rect = new global::Android.Graphics.Rect();
            _host.GetWindowVisibleDisplayFrame(rect);

            int heightDiff = _host.RootView.Height - (rect.Bottom - rect.Top);
            var isKeyboard = heightDiff >= estimatedKeyboardHeight;

            if (_wasKeyboard && !isKeyboard)
                KeyboardDevice.Instance.SetFocusedElement(null, NavigationMethod.Unspecified, KeyModifiers.None);

            _wasKeyboard = isKeyboard;
        }
    }
}
