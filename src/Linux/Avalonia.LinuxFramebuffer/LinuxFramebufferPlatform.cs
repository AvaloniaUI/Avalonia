using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Embedding;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.LinuxFramebuffer;
using Avalonia.LinuxFramebuffer.Input;
using Avalonia.LinuxFramebuffer.Input.EvDev;
using Avalonia.LinuxFramebuffer.Input.LibInput;
using Avalonia.LinuxFramebuffer.Output;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;

#nullable enable

namespace Avalonia.LinuxFramebuffer
{
    internal class LinuxFramebufferIconLoaderStub : IPlatformIconLoader
    {
        private class IconStub : IWindowIconImpl
        {
            public void Save(Stream outputStream)
            {

            }
        }

        public IWindowIconImpl LoadIcon(string fileName) => new IconStub();

        public IWindowIconImpl LoadIcon(Stream stream) => new IconStub();

        public IWindowIconImpl LoadIcon(IBitmapImpl bitmap) => new IconStub();
    }
    
    class LinuxFramebufferPlatform
    {
        IOutputBackend _fb;
        public static ManualRawEventGrouperDispatchQueue EventGrouperDispatchQueue = new();

        internal static Compositor Compositor { get; private set; } = null!;
       
        
        LinuxFramebufferPlatform(IOutputBackend backend)
        {
            _fb = backend;
        }
        
        void Initialize()
        {
            if (_fb is IGlOutputBackend gl)
                AvaloniaLocator.CurrentMutable.Bind<IPlatformGraphics>().ToConstant(gl.PlatformGraphics);

            var opts = AvaloniaLocator.Current.GetService<LinuxFramebufferPlatformOptions>() ?? new LinuxFramebufferPlatformOptions();

            AvaloniaLocator.CurrentMutable
                .Bind<IDispatcherImpl>().ToConstant(new ManagedDispatcherImpl(new ManualRawEventGrouperDispatchQueueDispatcherInputProvider(EventGrouperDispatchQueue)))
                .Bind<IRenderTimer>().ToConstant(new DefaultRenderTimer(opts.Fps))
                .Bind<ICursorFactory>().ToTransient<CursorFactoryStub>()
                .Bind<IKeyboardDevice>().ToConstant(new KeyboardDevice())
                .Bind<IPlatformIconLoader>().ToSingleton<LinuxFramebufferIconLoaderStub>()
                .Bind<IPlatformSettings>().ToSingleton<DefaultPlatformSettings>()
                .Bind<PlatformHotkeyConfiguration>().ToSingleton<PlatformHotkeyConfiguration>();
            
            Compositor = new Compositor(AvaloniaLocator.Current.GetService<IPlatformGraphics>());
        }


        internal static LinuxFramebufferLifetime Initialize(AppBuilder builder, IOutputBackend outputBackend, IInputBackend? inputBackend)
        {
            var platform = new LinuxFramebufferPlatform(outputBackend);
            builder
                .UseStandardRuntimePlatformSubsystem()
                .UseSkia()
                .UseWindowingSubsystem(platform.Initialize, "fbdev");
            return new LinuxFramebufferLifetime(platform._fb, inputBackend);
        }
    }

    class LinuxFramebufferLifetime : IControlledApplicationLifetime, ISingleViewApplicationLifetime
    {
        private readonly IOutputBackend _fb;
        private readonly IInputBackend? _inputBackend;
        private EmbeddableControlRoot? _topLevel;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        public CancellationToken Token => _cts.Token;

        public LinuxFramebufferLifetime(IOutputBackend fb)
        {
            _fb = fb;
        }

        public LinuxFramebufferLifetime(IOutputBackend fb, IInputBackend? input)
        {
            _fb = fb;
            _inputBackend = input;
        }

        public Control? MainView
        {
            get => (Control?)_topLevel?.Content;
            set
            {
                if (_topLevel == null)
                {
                    var inputBackend = _inputBackend;
                    if (inputBackend == null)
                    {
                        if (Environment.GetEnvironmentVariable("AVALONIA_USE_EVDEV") == "1")
                            inputBackend = EvDevBackend.CreateFromEnvironment();
                        else
                            inputBackend = new LibInputBackend();
                    }

                    var tl = new EmbeddableControlRoot(new FramebufferToplevelImpl(_fb, inputBackend));
                    tl.Prepare();
                    tl.StartRendering();
                    _topLevel = tl;
                    

                    if (_topLevel is IFocusScope scope && _topLevel.FocusManager is FocusManager focusManager)
                    {
                        focusManager.SetFocusScope(scope);
                    }
                }

                _topLevel.Content = value;
            }
        }

        public int ExitCode { get; private set; }
        public event EventHandler<ControlledApplicationLifetimeStartupEventArgs>? Startup;
        public event EventHandler<ControlledApplicationLifetimeExitEventArgs>? Exit;

        public void Start(string[] args)
        {
            Startup?.Invoke(this, new ControlledApplicationLifetimeStartupEventArgs(args));
        }

        public void Shutdown(int exitCode)
        {
            ExitCode = exitCode;
            var e = new ControlledApplicationLifetimeExitEventArgs(exitCode);
            Exit?.Invoke(this, e);
            ExitCode = e.ApplicationExitCode;
            _cts.Cancel();
        }
    }
}

public static class LinuxFramebufferPlatformExtensions
{
    public static int StartLinuxFbDev(this AppBuilder builder, string[] args, string? fbdev = null, double scaling = 1, IInputBackend? inputBackend = default)
        => StartLinuxDirect(builder, args, new FbdevOutput(fileName: fbdev, format: null) { Scaling = scaling }, inputBackend);
    public static int StartLinuxFbDev(this AppBuilder builder, string[] args, string fbdev, PixelFormat? format, double scaling, IInputBackend? inputBackend = default)
        => StartLinuxDirect(builder, args, new FbdevOutput(fileName: fbdev, format: format) { Scaling = scaling }, inputBackend);

    public static int StartLinuxDrm(this AppBuilder builder, string[] args, string? card = null, double scaling = 1, IInputBackend? inputBackend = default)
        => StartLinuxDirect(builder, args, new DrmOutput(card) { Scaling = scaling }, inputBackend);
    public static int StartLinuxDrm(this AppBuilder builder, string[] args, string? card = null, bool connectorsForceProbe = false, DrmOutputOptions? options = null, IInputBackend? inputBackend = default)
        => StartLinuxDirect(builder, args, new DrmOutput(card, connectorsForceProbe, options), inputBackend);

    public static int StartLinuxDirect(this AppBuilder builder, string[] args, IOutputBackend outputBackend, IInputBackend? inputBackend = default)
    {
        var lifetime = LinuxFramebufferPlatform.Initialize(builder, outputBackend, inputBackend);
        builder.SetupWithLifetime(lifetime);
        lifetime.Start(args);
        builder.Instance!.Run(lifetime.Token);
        return lifetime.ExitCode;
    }
}

