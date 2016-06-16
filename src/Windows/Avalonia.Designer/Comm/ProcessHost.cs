using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace Avalonia.Designer.Comm
{
    class ProcessHost : INotifyPropertyChanged
    {
        private CommChannel _comm;
        private string _state;
        public string State
        {
            get { return _state; }
            set
            {
                if (_state == value)
                    return;
                _state = value;
                OnPropertyChanged();
            }
        }
        
        private bool _isAlive;
        private readonly SynchronizationContext _dispatcher;
        private Process _proc;

        public bool IsAlive
        {
            get { return _isAlive; }
            set
            {
                if (_isAlive == value)
                    return;
                _isAlive = value;
                OnPropertyChanged();
            }
        }

        private IntPtr _windowHandle;
        public IntPtr WindowHandle
        {
            get { return _windowHandle; }
            set
            {
                if (_windowHandle == value)
                    return;
                _windowHandle = value;
                OnPropertyChanged();
            }
        }

        public ProcessHost()
        {
            _dispatcher = SynchronizationContext.Current;
        }

        void OnExited(object sender, EventArgs eventArgs)
        {
            if(_proc != sender)
                return;
            _proc = null;
            _dispatcher.Post(_ =>
            {
                
                HandleExited();
            }, null);
        }

        void HandleExited()
        {
            _comm.Dispose();
            IsAlive = false;
            WindowHandle = IntPtr.Zero;
            State = "Designer process crashed";
        }

        public void Start(string targetExe, string initialXaml, string sourceAssembly)
        {
            if (_proc != null)
            {
                _proc.Exited -= OnExited;
                try
                {
                    _comm?.Dispose();
                    _proc.Kill();
                }
                catch { }
                HandleExited();
                State = "Restarting...";
            }

            var msg = new InitMessage(Path.GetFullPath(targetExe), initialXaml, sourceAssembly);
            var exe = typeof (ProcessHost).Assembly.GetModules()[0].FullyQualifiedName;
            _proc = new Process()
            {
                StartInfo = new ProcessStartInfo(exe, "--hosted")
                {
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true
            };
            _proc.Exited += OnExited;
            try
            {
                _proc.Start();
                State = "Launching designer process...";
                _comm = new CommChannel(_proc.StandardOutput.BaseStream, _proc.StandardInput.BaseStream);
                _comm.OnMessage += OnMessage;
                _comm.Start();
                _comm.SendMessage(msg);
            }
            catch (Exception e)
            {
                State = e.ToString();
                HandleExited();
            }
            IsAlive = true;

        }

        public void UpdateXaml(string xaml, string sourceAssembly)
        {
            _comm?.SendMessage(new UpdateXamlMessage(xaml, sourceAssembly));
        }

        private void OnMessage(object obj)
        {
            var stateMessage = obj as StateMessage;
            if (stateMessage != null)
                State = stateMessage.State;
            var windowMessage = obj as WindowCreatedMessage;
            if (windowMessage != null)
                WindowHandle = windowMessage.Handle;
        }

        public void Kill()
        {
            try
            {
                _proc?.Kill();
                _proc = null;
            }
            catch
            {
                //
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
