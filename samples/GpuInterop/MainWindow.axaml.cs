using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Rendering;
using GpuInterop.D3DDemo;
using GpuInterop.VulkanDemo;

namespace GpuInterop
{
    public class MainWindow : Window
    {
        public MainWindow() : this(DemoType.Vulkan)
        {
        }

        public MainWindow(DemoType demoType)
        {
            InitializeComponent();

            Title = demoType.ToString();

            Content = new GpuDemo
            {
                Demo = demoType switch
                {
                    DemoType.Vulkan => new VulkanDemoControl(),
                    DemoType.D3D11 => new D3D11DemoControl(),
                    var unknown => throw new InvalidOperationException($"Unknown demo type {unknown}")
                }
            };

            RendererDiagnostics.DebugOverlays = RendererDebugOverlays.Fps;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
