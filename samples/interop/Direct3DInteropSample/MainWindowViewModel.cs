using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReactiveUI;

namespace Direct3DInteropSample
{
    public class MainWindowViewModel : ReactiveObject
    {
        private double _rotationX;

        public double RotationX
        {
            get { return _rotationX; }
            set { this.RaiseAndSetIfChanged(ref _rotationX, value); }
        }

        private double _rotationY = 1;

        public double RotationY
        {
            get { return _rotationY; }
            set { this.RaiseAndSetIfChanged(ref _rotationY, value); }
        }

        private double _rotationZ = 2;

        public double RotationZ
        {
            get { return _rotationZ; }
            set { this.RaiseAndSetIfChanged(ref _rotationZ, value); }
        }


        private double _zoom = 1;

        public double Zoom
        {
            get { return _zoom; }
            set { this.RaiseAndSetIfChanged(ref _zoom, value); }
        }
    }
}
