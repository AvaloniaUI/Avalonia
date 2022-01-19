using System;
using MiniMvvm;
using Avalonia.Animation;

namespace RenderDemo.ViewModels
{
    public class Transform3DPageViewModel : ViewModelBase
    {
        private double _depth = 200;

        public double Depth
        {
            get => _depth;
            set => RaiseAndSetIfChanged(ref _depth, value);
        }
    }
}
