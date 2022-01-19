using System;
using MiniMvvm;
using Avalonia.Animation;

namespace RenderDemo.ViewModels
{
    public class Transform3DPageViewModel : ViewModelBase
    {
        private double _rotationX = 0;
        private double _rotationY = 0;
        private double _rotationZ = 0;
        
        private double _x = 0;
        private double _y = 0;
        private double _z = 0;
        
        private double _depth = 200;

        public double RotationX
        {
            get => _rotationX;
            set => RaiseAndSetIfChanged(ref _rotationX, value);
        }
        
        public double RotationY
        {
            get => _rotationY;
            set => RaiseAndSetIfChanged(ref _rotationY, value);
        }
        
        public double RotationZ
        {
            get => _rotationZ;
            set => RaiseAndSetIfChanged(ref _rotationZ, value);
        }
        
        public double Depth
        {
            get => _depth;
            set => RaiseAndSetIfChanged(ref _depth, value);
        }
        
        public double X
        {
            get => _x;
            set => RaiseAndSetIfChanged(ref _x, value);
        }
        
        public double Y
        {
            get => _y;
            set => RaiseAndSetIfChanged(ref _y, value);
        }
        
        public double Z
        {
            get => _z;
            set => RaiseAndSetIfChanged(ref _z, value);
        }
    }
}
