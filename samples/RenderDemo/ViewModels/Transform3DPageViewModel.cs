using System;
using MiniMvvm;
using Avalonia.Animation;

namespace RenderDemo.ViewModels
{
    public class Transform3DPageViewModel : ViewModelBase
    {
        private double _depth = 200;

        private double _centerX = 0;
        private double _centerY = 0;
        private double _centerZ = 0;
        private double _angleX = 0;
        private double _angleY = 0;
        private double _angleZ = 0;
        
        public double Depth
        {
            get => _depth;
            set => RaiseAndSetIfChanged(ref _depth, value);
        }
        
        public double CenterX
        {
            get => _centerX;
            set => RaiseAndSetIfChanged(ref _centerX, value);
        }
        public double CenterY
        {
            get => _centerY;
            set => RaiseAndSetIfChanged(ref _centerY, value);
        }
        public double CenterZ
        {
            get => _centerZ;
            set => RaiseAndSetIfChanged(ref _centerZ, value);
        }
        public double AngleX
        {
            get => _angleX;
            set => RaiseAndSetIfChanged(ref _angleX, value);
        }
        public double AngleY
        {
            get => _angleY;
            set => RaiseAndSetIfChanged(ref _angleY, value);
        }
        public double AngleZ
        {
            get => _angleZ;
            set => RaiseAndSetIfChanged(ref _angleZ, value);
        }
    }
}
