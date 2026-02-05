using System;
using Avalonia.Input;
using Avalonia.Input.TextInput;
using Avalonia.Logging;
using Avalonia.Platform;
using Avalonia.Rendering;

namespace Avalonia.Controls;

internal partial class PresentationSource : IPresentationSource, IDisposable
{
    public ITopLevelImpl? PlatformImpl { get; private set; }
    private readonly PointerOverPreProcessor? _pointerOverPreProcessor;
    private readonly IDisposable? _pointerOverPreProcessorSubscription;
    private readonly IInputManager? _inputManager;
    

    internal FocusManager FocusManager { get; } = new();

    public PresentationSource(InputElement rootVisual, ITopLevelImpl platformImpl, IAvaloniaDependencyResolver dependencyResolver)
    {
        RootVisual = rootVisual;
        PlatformImpl = platformImpl;

    
        _inputManager = TryGetService<IInputManager>(dependencyResolver);
        _handleInputCore = HandleInputCore;
        
        PlatformImpl.SetInputRoot(this);
        PlatformImpl.Input = HandleInput;
        
        _pointerOverPreProcessor = new PointerOverPreProcessor(this);
        _pointerOverPreProcessorSubscription = _inputManager?.PreProcess.Subscribe(_pointerOverPreProcessor);
    }

    // In WPF it's a Visual and it's nullable. For now we have it as non-nullable InputElement since 
    // there are way too many things to update at once and the current goal is to decouple
    // "visual tree root" concept from TopLevel
    public InputElement RootVisual
    {
        get => field;
        set
        {
            field = value;
            FocusManager.SetContentRoot(value as IInputElement);
        }
    }


    IFocusManager? IInputRoot.FocusManager => FocusManager;

    IPlatformSettings? IPresentationSource.PlatformSettings => AvaloniaLocator.Current.GetService<IPlatformSettings>();

    IInputElement? IInputRoot.PointerOverElement
    {
        get => field;
        set
        {
            field = value;
            SetCursor(value?.Cursor);
        }
    }


    ITextInputMethodImpl? IInputRoot.InputMethod => PlatformImpl?.TryGetFeature<ITextInputMethodImpl>();
    public InputElement RootElement => RootVisual;


    public void Dispose()
    {
        PlatformImpl = null;
        _pointerOverPreProcessor?.OnCompleted();
        _pointerOverPreProcessorSubscription?.Dispose();
    }
    
    /// <summary>
    /// Tries to get a service from an <see cref="IAvaloniaDependencyResolver"/>, logging a
    /// warning if not found.
    /// </summary>
    /// <typeparam name="T">The service type.</typeparam>
    /// <param name="resolver">The resolver.</param>
    /// <returns>The service.</returns>
    private T? TryGetService<T>(IAvaloniaDependencyResolver resolver) where T : class
    {
        var result = resolver.GetService<T>();

        if (result == null)
        {
            Logger.TryGet(LogEventLevel.Warning, LogArea.Control)?.Log(
                this,
                "Could not create {Service} : maybe Application.RegisterServices() wasn't called?",
                typeof(T));
        }

        return result;
    }

    // TODO: wire up directly to renderer after moving it here
    public void SceneInvalidated(Rect dirtyRect)
    {
        _pointerOverPreProcessor?.SceneInvalidated(dirtyRect);
    }

    // TODO: Make popup positioner to use PresentationSource internally rather than TopLevel
    public PixelPoint? GetLastPointerPosition(Visual topLevel)
    {
        return _pointerOverPreProcessor?.LastPosition;
    }
}