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
                case AnimationPlayState.Running:
                    PlayStateText = "Resume all animations";
                    Timing.SetGlobalPlayState(AnimationPlayState.Paused);
                    break;

                case AnimationPlayState.Paused:
                    PlayStateText = "Pause all animations";
                    Timing.SetGlobalPlayState(AnimationPlayState.Running);
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
