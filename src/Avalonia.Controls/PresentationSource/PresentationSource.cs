using System;
using Avalonia.Input;
using Avalonia.Input.TextInput;
using Avalonia.Layout;
using Avalonia.Logging;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;

namespace Avalonia.Controls;

internal partial class PresentationSource : IPresentationSource, IInputRoot, IDisposable
{
    public ITopLevelImpl? PlatformImpl { get; private set; }
    private readonly PointerOverPreProcessor? _pointerOverPreProcessor;
    private readonly IDisposable? _pointerOverPreProcessorSubscription;
    private readonly IInputManager? _inputManager;
    

    internal FocusManager FocusManager { get; } = new();

    public PresentationSource(InputElement rootVisual, ITopLevelImpl platformImpl,
        IAvaloniaDependencyResolver dependencyResolver, Func<Size> clientSizeProvider)
    {
        _clientSizeProvider = clientSizeProvider;

        PlatformImpl = platformImpl;

    
        _inputManager = TryGetService<IInputManager>(dependencyResolver);
        _handleInputCore = HandleInputCore;
        
        PlatformImpl.SetInputRoot(this);
        PlatformImpl.Input = HandleInput;
        
        _pointerOverPreProcessor = new PointerOverPreProcessor(this);
        _pointerOverPreProcessorSubscription = _inputManager?.PreProcess.Subscribe(_pointerOverPreProcessor);
        
        Renderer = new CompositingRenderer(this, PlatformImpl.Compositor, () => PlatformImpl.Surfaces ?? []);
        Renderer.SceneInvalidated += SceneInvalidated;
        LayoutManager = CreateLayoutManager();
        
        RootVisual = rootVisual;
    }

    // In WPF it's a Visual and it's nullable. For now we have it as non-nullable InputElement since 
    // there are way too many things to update at once and the current goal is to decouple
    // "visual tree root" concept from TopLevel
    public InputElement RootVisual
    {
        get => field;
        set
        {
            field?.SetPresentationSourceForRootVisual(null);
            field = value;

            field?.SetPresentationSourceForRootVisual(this);
            Renderer.CompositionTarget.Root = field?.CompositionVisual;

            FocusManager.SetContentRoot(value as IInputElement);
        }
    }


    IFocusManager? IInputRoot.FocusManager => FocusManager;

    IPlatformSettings? IPresentationSource.PlatformSettings => AvaloniaLocator.Current.GetService<IPlatformSettings>();
    
    ITextInputMethodImpl? IInputRoot.InputMethod => PlatformImpl?.TryGetFeature<ITextInputMethodImpl>();
    public InputElement RootElement => RootVisual;


    public void Dispose()
    {
        _layoutDiagnosticBridge?.Dispose();
        _layoutDiagnosticBridge = null;
        LayoutManager.Dispose();
        Renderer.SceneInvalidated -= SceneInvalidated;
        // We need to wait for the renderer to complete any in-flight operations
        Renderer.Dispose();

        PlatformImpl = null;
        _pointerOverPreProcessor?.OnCompleted();
        _pointerOverPreProcessorSubscription?.Dispose();
        if (((IInputRoot)this).PointerOverElement is AvaloniaObject pointerOverElement)
            pointerOverElement.PropertyChanged -= PointerOverElement_PropertyChanged;
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

    // TODO: Make popup positioner to use PresentationSource internally rather than TopLevel
    public PixelPoint? GetLastPointerPosition(Visual topLevel)
    {
        return _pointerOverPreProcessor?.LastPosition;
    }
}