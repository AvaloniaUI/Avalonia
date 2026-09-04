using Avalonia.Controls;
using MiniMvvm;

namespace ControlCatalog.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        public SettingsViewModel()
        {
            WindowStates = new WindowState[]
            {
                WindowState.Minimized,
                WindowState.Normal,
                WindowState.Maximized,
                WindowState.FullScreen,
            };

            WindowState = WindowState.Normal;
        }

        public WindowState WindowState
        {
            get;
            set => RaiseAndSetIfChanged(ref field, value);
        }

        public int SelectedDecorationIndex
        {
            get;
            set => RaiseAndSetIfChanged(ref field, value);
        }

        public int SelectedThemeVariantIndex
        {
            get;
            set => RaiseAndSetIfChanged(ref field, value);
        }

        public int SelectedTransparencyLevelIndex
        {
            get;
            set => RaiseAndSetIfChanged(ref field, value);
        }

        public int SelectedFlowDirectionIndex
        {
            get;
            set => RaiseAndSetIfChanged(ref field, value);
        }

        public WindowState[] WindowStates
        {
            get;
            set => RaiseAndSetIfChanged(ref field, value);
        }
    }
}
