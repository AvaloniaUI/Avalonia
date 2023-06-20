using MiniMvvm;

namespace VirtualizationDemo.ViewModels;

internal class MainWindowViewModel : ViewModelBase
{
    public PlaygroundPageViewModel Playground { get; } = new();
    public ChatPageViewModel Chat { get; } = new();
    public ExpanderPageViewModel Expanders { get; } = new();
}
