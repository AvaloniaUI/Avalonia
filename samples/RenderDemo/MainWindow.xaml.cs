using System;
using System.Linq.Expressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Rendering;
using RenderDemo.ViewModels;
using MiniMvvm;

namespace RenderDemo
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.AttachDevTools();

            var vm = new MainWindowViewModel();

            void BindOverlay(Expression<Func<MainWindowViewModel, bool>> expr, RendererDebugOverlays overlay)
                => vm.WhenAnyValue(expr).Subscribe(x =>
                {
                    var diagnostics = RendererDiagnostics;
                    diagnostics.DebugOverlays = x ?
                        diagnostics.DebugOverlays | overlay :
                        diagnostics.DebugOverlays & ~overlay;
                });

            BindOverlay(x => x.DrawDirtyRects, RendererDebugOverlays.DirtyRects);
            BindOverlay(x => x.DrawFps, RendererDebugOverlays.Fps);
            BindOverlay(x => x.DrawLayoutTimeGraph, RendererDebugOverlays.LayoutTimeGraph);
            BindOverlay(x => x.DrawRenderTimeGraph, RendererDebugOverlays.RenderTimeGraph);

            DataContext = vm;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
