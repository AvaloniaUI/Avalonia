using System;
using System.ComponentModel;
using System.Windows.Forms;
using Avalonia.Controls;
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

        public WinFormsAvaloniaControlHost()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            UnmanagedMethods.SetParent(_root.PlatformImpl.Handle.Handle, Handle);
            _root.Prepare();
            if (_root.IsFocused)
                FocusManager.Instance.Focus(null);
            _root.GotFocus += RootGotFocus;
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
            UnmanagedMethods.SetFocus(_root.PlatformImpl.Handle.Handle);
        }

        protected override void OnGotFocus(EventArgs e)
        {
            if (_root != null)
                UnmanagedMethods.SetFocus(_root.PlatformImpl.Handle.Handle);
        }


        void FixPosition()
        {
            if (_root != null && Width > 0 && Height > 0)
                UnmanagedMethods.MoveWindow(_root.PlatformImpl.Handle.Handle, 0, 0, Width, Height, true);
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
