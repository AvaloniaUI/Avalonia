using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Perspex.Designer.AppHost;
using Perspex.Designer.Comm;
using Perspex.Designer.Metadata;

namespace Perspex.Designer
{
    /// <summary>
    /// Interaction logic for PerpexDesigner.xaml
    /// </summary>
    public partial class PerspexDesigner
    {
        public static readonly DependencyProperty TargetExeProperty = DependencyProperty.Register(
            "TargetExe", typeof (string), typeof (PerspexDesigner), new FrameworkPropertyMetadata(TargetExeChanged));

        private static void TargetExeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((PerspexDesigner) d).RestartProcess();
        }
        public string TargetExe
        {
            get { return (string) GetValue(TargetExeProperty); }
            set { SetValue(TargetExeProperty, value); }
        }

        public static readonly DependencyProperty XamlProperty = DependencyProperty.Register(
            "Xaml", typeof (string), typeof (PerspexDesigner), new FrameworkPropertyMetadata(XamlChanged));

        private static void XamlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((PerspexDesigner) d).OnXamlChanged();
        }

        public string Xaml
        {
            get { return (string) GetValue(XamlProperty); }
            set { SetValue(XamlProperty, value); }
        }

        public PerspexDesignerMetadata Metadata { get; private set; }
        
        private readonly ProcessHost _host = new ProcessHost();
        

        public PerspexDesigner()
        {
            InitializeComponent();
            BindingOperations.SetBinding(State, TextBox.TextProperty,
                new Binding(nameof(ProcessHost.State)) {Source = _host, Mode = BindingMode.OneWay});

            _host.PropertyChanged += _host_PropertyChanged;
            _host.MetadataArrived += data => Metadata = data;
        }

        private void _host_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ProcessHost.WindowHandle))
            {
                if (NativeContainer.Content != null)
                {
                    var wndHost = ((HwndHost) NativeContainer.Content);
                    NativeContainer.Content = null;
                    wndHost?.Dispose();
                }
                if (_host.WindowHandle != IntPtr.Zero)
                {
                    var host = new NativeWindowHost(_host.WindowHandle);
                    NativeContainer.Content = host;
                }
            }
        }

        class NativeWindowHost  :HwndHost
        {
            private readonly IntPtr _hWnd;

            public NativeWindowHost(IntPtr hWnd)
            {
                _hWnd = hWnd;
            }

            protected override HandleRef BuildWindowCore(HandleRef hwndParent)
            {
                WinApi.SetParent(_hWnd, hwndParent.Handle);
                return new HandleRef(this, _hWnd);
            }

            protected override void DestroyWindowCore(HandleRef hwnd)
            {
                WinApi.SendMessage(hwnd.Handle, WinApi.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            }
        }

        public void KillProcess()
        {
            _host.Kill();
        }

        bool CheckTargetExeOrSetError()
        {
            if (string.IsNullOrEmpty(TargetExe))
            {
                _host.State = "No target exe found";
                return false;
            }

            if (File.Exists(TargetExe ?? ""))
                return true;
            _host.State = "No target binary found, build your project";
            return false;
        }

        public void RestartProcess()
        {
            KillProcess();
            if(!CheckTargetExeOrSetError())
                return;
            if(string.IsNullOrEmpty(Xaml))
                return;
            _host.Start(TargetExe, Xaml);
        }

        private void OnXamlChanged()
        {
            if (!CheckTargetExeOrSetError())
                return;
            if (!_host.IsAlive)
                _host.Start(TargetExe, Xaml);
            else
                _host.UpdateXaml(Xaml ?? "");
        }

    }
}
