using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using JetBrains.Annotations;

namespace Avalonia.Designer.AppHost
{
    public class HostedAppModel : INotifyPropertyChanged
    {
        private readonly AvaloniaAppHost _host;
        private IntPtr _nativeWindowHandle;
        private string _error;
        private string _errorDetails;

        internal HostedAppModel(AvaloniaAppHost host)
        {
            _host = host;
            Background = Settings.Background;
        }

        public IntPtr NativeWindowHandle
        {
            get { return _nativeWindowHandle; }
            set
            {
                if (value.Equals(_nativeWindowHandle)) return;
                _nativeWindowHandle = value;
                OnPropertyChanged();
            }
        }

        public string Error
        {
            get { return _error; }
            private set
            {
                if (value == _error) return;
                _error = value;
                OnPropertyChanged();
            }
        }

        public string ErrorDetails
        {
            get { return _errorDetails; }
            private set
            {
                if (value == _errorDetails) return;
                _errorDetails = value;
                OnPropertyChanged();
            }
        }

        public string Background
        {
            get { return _background; }
            set
            {
                if (value == _background) return;
                _background = value;
                OnPropertyChanged();
            }
        }

        public IReadOnlyList<double> AvailableScalingFactors => new List<double>() {1, 2, 4, 8};

        public double CurrentScalingFactor
        {
            get { return _currentScalingFactor; }
            set
            {
                _currentScalingFactor = value;
                _host.Api.SetScalingFactor(value);
            }
        }

        public void SetError(string error, string details = null)
        {
            Error = error;
            ErrorDetails = details;
        }

        double _currentScalingFactor = 1;
        private string _background;

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
