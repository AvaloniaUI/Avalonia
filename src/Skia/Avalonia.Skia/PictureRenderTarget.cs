using System;
using Avalonia.Platform;
using Avalonia.Reactive;
using SkiaSharp;

namespace Avalonia.Skia;

internal class PictureRenderTarget : IDisposable
{
    private readonly ISkiaGpu? _gpu;
    private readonly GRContext? _grContext;
    private readonly Vector _dpi;
    private SKPicture? _picture;

    public PictureRenderTarget(ISkiaGpu? gpu, GRContext? grContext, Vector dpi)
    {
        _gpu = gpu;
        _grContext = grContext;
        _dpi = dpi;
    }

    public SKPicture GetPicture()
    {
        var rv = _picture ?? throw new InvalidOperationException();
        _picture = null;
        return rv;
    }
    
    public IDrawingContextImpl CreateDrawingContext(Size size, bool scaleToDpi = true)
    {
        if (scaleToDpi)
            size *= (_dpi / 96);
        var recorder = new SKPictureRecorder();
        var canvas = recorder.BeginRecording(new SKRect(0, 0, (float)size.Width,
            (float)size.Height));
        
        canvas.RestoreToCount(-1);
        canvas.ResetMatrix();
            
        var createInfo = new DrawingContextImpl.CreateInfo
        {
            Canvas = canvas,
            ScaleDrawingToDpi = scaleToDpi,
            Dpi = _dpi,
            DisableSubpixelTextRendering = true,
            GrContext = _grContext,
            Gpu = _gpu,
        };
        return new DrawingContextImpl(createInfo, Disposable.Create(() =>
        {
            _picture = recorder.EndRecording();
            canvas.Dispose();
            recorder.Dispose();
        }));
    }

    public void Dispose() => _picture?.Dispose();
}
