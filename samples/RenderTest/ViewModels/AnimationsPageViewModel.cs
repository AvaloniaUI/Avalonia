using System;
using ReactiveUI;
using Avalonia.Animation;

namespace RenderTest.ViewModels
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
                case PlayState.Running:
                    PlayStateText = "Resume all animations";
                    Timing.SetGlobalPlayState(PlayState.Paused);
                    break;

                case PlayState.Paused:
                    PlayStateText = "Pause all animations";
                    Timing.SetGlobalPlayState(PlayState.Running);
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
