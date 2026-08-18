using System;
using System.ComponentModel;
using Avalonia.Layout;
using Avalonia.Rendering;

namespace Avalonia.Controls;

internal partial class PresentationSource : ILayoutRoot
{
    private LayoutDiagnosticBridge? _layoutDiagnosticBridge;
    public double LayoutScaling => RenderScaling;
    public ILayoutManager LayoutManager { get; }
    ILayoutRoot IPresentationSource.LayoutRoot => this;
    Layoutable ILayoutRoot.RootVisual => RootVisual;

    private ILayoutManager CreateLayoutManager()
    {
        var manager = new LayoutManager(this);
        _layoutDiagnosticBridge = new LayoutDiagnosticBridge(Renderer.Diagnostics, manager);
        _layoutDiagnosticBridge.SetupBridge();
        return manager;
    }


    /// <summary>
    /// Provides layout pass timing from the layout manager to the renderer, for diagnostics purposes.
    /// </summary>
    private sealed class LayoutDiagnosticBridge : IDisposable
    {
        private readonly RendererDiagnostics _diagnostics;
        private readonly LayoutManager _layoutManager;
        private bool _isHandling;

        public LayoutDiagnosticBridge(RendererDiagnostics diagnostics, LayoutManager layoutManager)
        {
            _diagnostics = diagnostics;
            _layoutManager = layoutManager;

            diagnostics.PropertyChanged += OnDiagnosticsPropertyChanged;
        }

        public void SetupBridge()
        {
            var needsHandling = (_diagnostics.DebugOverlays & RendererDebugOverlays.LayoutTimeGraph) != 0;
            if (needsHandling != _isHandling)
            {
                _isHandling = needsHandling;
                _layoutManager.LayoutPassTimed = needsHandling
                    ? timing => _diagnostics.LastLayoutPassTiming = timing
                    : null;
            }
        }

        private void OnDiagnosticsPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(RendererDiagnostics.DebugOverlays))
            {
                SetupBridge();
            }
        }

        public void Dispose()
        {
            _diagnostics.PropertyChanged -= OnDiagnosticsPropertyChanged;
            _layoutManager.LayoutPassTimed = null;
        }
    }
    
}