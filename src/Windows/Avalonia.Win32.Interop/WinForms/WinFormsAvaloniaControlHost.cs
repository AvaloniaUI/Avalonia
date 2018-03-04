using System;
using System.ComponentModel;
using System.Windows.Forms;
using Avalonia.Controls.Embedding;
using Avalonia.Input;
using Avalonia.VisualTree;
using Avalonia.Win32.Interop;
using WinFormsControl = System.Windows.Forms.Control;

namespace Avalonia.Win32.Embedding
{
    [ToolboxItem(true)]
    public class WinFormsAvaloniaControlHost : WinFormsControl
    {
        private readonly EmbeddableControlRoot _root = new EmbeddableControlRoot();

        private IntPtr WindowHandle => ((WindowImpl) _root?.PlatformImpl)?.Handle?.Handle ?? IntPtr.Zero;

        public WinFormsAvaloniaControlHost()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            UnmanagedMethods.SetParent(WindowHandle, Handle);
            _root.Prepare();
            if (_root.IsFocused)
                FocusManager.Instance.Focus(null);
            _root.GotFocus += RootGotFocus;
            // ReSharper disable once PossibleNullReferenceException
            // Always non-null at this point
            _root.PlatformImpl.LostFocus += PlatformImpl_LostFocus;
            FixPosition();
        }

        public Avalonia.Controls.Control Content
        {
            get { return (Avalonia.Controls.Control)_root.Content; }
            set { _root.Content = value; }
        }

        void Unfocus()
        {
            var focused = (IVisual)FocusManager.Instance.Current;
            if (focused == null)
                return;
            while (focused.VisualParent != null)
                focused = focused.VisualParent;

            if (focused == _root)
                KeyboardDevice.Instance.SetFocusedElement(null, NavigationMethod.Unspecified, InputModifiers.None);
        }

        private void PlatformImpl_LostFocus()
        {
            Unfocus();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _root.Dispose();
            base.Dispose(disposing);
        }

        private void RootGotFocus(object sender, Interactivity.RoutedEventArgs e)
        {
            UnmanagedMethods.SetFocus(WindowHandle);
        }

        protected override void OnGotFocus(EventArgs e)
        {
            if (_root != null)
                UnmanagedMethods.SetFocus(WindowHandle);
        }


        void FixPosition()
        {
            if (_root != null && Width > 0 && Height > 0)
                UnmanagedMethods.MoveWindow(WindowHandle, 0, 0, Width, Height, true);
        }



        protected override void OnResize(EventArgs e)
        {
            FixPosition();
            base.OnResize(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {

        }
    }
}
