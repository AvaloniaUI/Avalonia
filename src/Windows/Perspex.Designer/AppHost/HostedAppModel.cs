using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Perspex.Designer.AppHost
{
    public class HostedAppModel : INotifyPropertyChanged
    {
        private readonly PerspexAppHost _host;
        private IntPtr _nativeWindowHandle;
        private string _error;
        private string _errorDetails;

        internal HostedAppModel(PerspexAppHost host)
        {
            _host = host;
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

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
