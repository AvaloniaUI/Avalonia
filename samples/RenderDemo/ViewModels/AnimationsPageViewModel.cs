using System;
using ReactiveUI;
using Avalonia.Animation;

namespace RenderDemo.ViewModels
{
    public class AnimationsPageViewModel : ReactiveObject
    {
        private string _playStateText = "Pause all animations";

        public AnimationsPageViewModel()
        {
            ToggleGlobalPlayState = ReactiveCommand.Create(()=>TogglePlayState());
        }

        void TogglePlayState()
        {
            switch (Timing.GetGlobalPlayState())
            {
                case PlayState.Run:
                    PlayStateText = "Resume all animations";
                    Timing.SetGlobalPlayState(PlayState.Pause);
                    break;

                case PlayState.Pause:
                    PlayStateText = "Pause all animations";
                    Timing.SetGlobalPlayState(PlayState.Run);
                    break;
            }
        }

        public string PlayStateText
        {
            get { return _playStateText; }
            set { this.RaiseAndSetIfChanged(ref _playStateText, value); }
        }

        public ReactiveCommand ToggleGlobalPlayState { get; }
     }
}
