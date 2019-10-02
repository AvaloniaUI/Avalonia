using System.Reactive.Linq;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using RenderDemo.ViewModels;

namespace RenderDemo.Pages
{
    public class AnimationsPage : UserControl
    {
        public AnimationsPage()
        {
            InitializeComponent();
            this.DataContext = new AnimationsPageViewModel();
            this._testControl = this.FindControl<Border>("TestControl");
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
        bool x;
        private Border _testControl;

        private void ToggleClock(object sender, RoutedEventArgs args)
        {
            var button = sender as Button;
            var clock = button.Clock;

            if (x)
            {
                _testControl.Classes.Add("Rect3");
            }
            else
            {
                _testControl.Classes.Remove("Rect3");
            }

            x = !x;

            // if (clock.PlayState == PlayState.Run)
            // {
            //     clock.PlayState = PlayState.Pause;
            // }
            // else if (clock.PlayState == PlayState.Pause)
            // {
            //     clock.PlayState = PlayState.Run;
            // }
        }
    }
}
