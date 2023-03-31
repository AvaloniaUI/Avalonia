using MiniMvvm;

namespace VirtualizationDemo.ViewModels;

internal class MainWindowViewModel : ViewModelBase
{
    public ChatPageViewModel Chat { get; } = new();
}
