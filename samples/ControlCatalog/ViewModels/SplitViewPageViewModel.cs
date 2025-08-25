using System;
using Avalonia.Controls;
using MiniMvvm;

namespace ControlCatalog.ViewModels
{
    public class SplitViewPageViewModel : ViewModelBase
    {
        private int _displayMode = 3; //CompactOverlay
        private int _placement = 0; //Left

        public int Placement
        {
            get => _placement;
            set
            {
                this.RaiseAndSetIfChanged(ref _placement, value);
                this.RaisePropertyChanged(nameof(PanePlacement));
            }
        }
        
        public int DisplayMode
        {
            get => _displayMode;
            set
            {
                this.RaiseAndSetIfChanged(ref _displayMode, value);
                this.RaisePropertyChanged(nameof(CurrentDisplayMode));
            }
        }

        public SplitViewPanePlacement PanePlacement =>
            _placement switch
            {
                0 => SplitViewPanePlacement.Left,
                1 => SplitViewPanePlacement.Right,
                2 => SplitViewPanePlacement.Top,
                3 => SplitViewPanePlacement.Bottom,
                _ => SplitViewPanePlacement.Left
            };
        
        public SplitViewDisplayMode CurrentDisplayMode
        {
            get
            {
                if (Enum.IsDefined(typeof(SplitViewDisplayMode), _displayMode))
                {
                    return (SplitViewDisplayMode)_displayMode;
                }
                return SplitViewDisplayMode.CompactOverlay;
            }
        }
    }
}
