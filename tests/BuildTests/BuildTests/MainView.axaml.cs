using System.Runtime.CompilerServices;
using Avalonia.Controls;

namespace BuildTests;

public partial class MainView : UserControl
{
    public string HelloText { get; set; }

    public MainView()
    {
        HelloText = $"Hello from {(RuntimeFeature.IsDynamicCodeSupported ? "JIT" : "AOT")}";
        DataContext = this;
        InitializeComponent();
    }
}
