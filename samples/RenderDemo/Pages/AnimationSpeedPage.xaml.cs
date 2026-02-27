using System.Reactive.Linq;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Markup.Xaml;
using RenderDemo.ViewModels;

namespace RenderDemo.Pages;

public class AnimationSpeedPage : UserControl
{
    public AnimationSpeedPage()
    {
        InitializeComponent();
        this.DataContext = new AnimationSpeedPageViewModel();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
