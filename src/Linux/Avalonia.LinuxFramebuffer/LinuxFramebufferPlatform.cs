using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
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
using Avalonia.OpenGL;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Threading;
using JetBrains.Annotations;

namespace Avalonia.LinuxFramebuffer
{
    class LinuxFramebufferPlatform
    {
        IOutputBackend _fb;
        private static readonly Stopwatch St = Stopwatch.StartNew();
        internal static uint Timestamp => (uint)St.ElapsedTicks;
        public static InternalPlatformThreadingInterface Threading;
        LinuxFramebufferPlatform(IOutputBackend backend)
        {
            _fb = backend;
        }


        void Initialize()
        {
            Threading = new InternalPlatformThreadingInterface();
            if (_fb is IGlOutputBackend gl)
                AvaloniaLocator.CurrentMutable.Bind<IPlatformOpenGlInterface>().ToConstant(gl.PlatformOpenGlInterface);
            
            var opts = AvaloniaLocator.Current.GetService<LinuxFramebufferPlatformOptions>() ?? new LinuxFramebufferPlatformOptions();
            
            AvaloniaLocator.CurrentMutable
                .Bind<IPlatformThreadingInterface>().ToConstant(Threading)
                .Bind<IRenderTimer>().ToConstant(new DefaultRenderTimer(opts.Fps))
                .Bind<IRenderLoop>().ToConstant(new RenderLoop())
                .Bind<ICursorFactory>().ToTransient<CursorFactoryStub>()
                .Bind<IKeyboardDevice>().ToConstant(new KeyboardDevice())
                .Bind<IPlatformSettings>().ToSingleton<PlatformSettings>()
                .Bind<PlatformHotkeyConfiguration>().ToSingleton<PlatformHotkeyConfiguration>();
        }

       
        internal static LinuxFramebufferLifetime Initialize<T>(T builder, IOutputBackend outputBackend) where T : AppBuilderBase<T>, new()
        {
            var platform = new LinuxFramebufferPlatform(outputBackend);
            builder.UseSkia().UseWindowingSubsystem(platform.Initialize, "fbdev");
            return new LinuxFramebufferLifetime(platform._fb);
        }
    }

    class LinuxFramebufferLifetime : IControlledApplicationLifetime, ISingleViewApplicationLifetime
    {
        private readonly IOutputBackend _fb;
        [CanBeNull] private readonly IInputBackend _inputBackend;
        private TopLevel _topLevel;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        public CancellationToken Token => _cts.Token;

        public LinuxFramebufferLifetime(IOutputBackend fb)
        {
            _fb = fb;
        }
        
        public LinuxFramebufferLifetime(IOutputBackend fb, IInputBackend input)
        {
            _fb = fb;
            _inputBackend = input;
        }
        
        public Control MainView
        {
            get => (Control)_topLevel?.Content;
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
                    _topLevel = tl;
                    _topLevel.Renderer.Start();

                    if (_topLevel is IFocusScope scope)
                    {
                        FocusManager.Instance?.SetFocusScope(scope);
                    }
                }

                _topLevel.Content = value;
            }
        }

        public int ExitCode { get; private set; }
        public event EventHandler<ControlledApplicationLifetimeStartupEventArgs> Startup;
        public event EventHandler<ControlledApplicationLifetimeExitEventArgs> Exit;

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
    public static int StartLinuxFbDev<T>(this T builder, string[] args, string fbdev = null, double scaling = 1)
        where T : AppBuilderBase<T>, new() =>
        StartLinuxDirect(builder, args, new FbdevOutput(fileName: fbdev, format: null) { Scaling = scaling });
    public static int StartLinuxFbDev<T>(this T builder, string[] args, string fbdev, PixelFormat? format, double scaling)
        where T : AppBuilderBase<T>, new() =>
        StartLinuxDirect(builder, args, new FbdevOutput(fileName: fbdev, format: format) { Scaling = scaling });

    public static int StartLinuxDrm<T>(this T builder, string[] args, string card = null, double scaling = 1)
        where T : AppBuilderBase<T>, new() => StartLinuxDirect(builder, args, CreateDrmOutput(card, 
        false, new DrmOutputOptions() { Scaling = scaling}) );
    public static int StartLinuxDrm<T>(this T builder, string[] args, string card = null, bool connectorsForceProbe = false, [CanBeNull] DrmOutputOptions options = null)
        where T : AppBuilderBase<T>, new() => StartLinuxDirect(builder, args, CreateDrmOutput(card, connectorsForceProbe, options));
    
    public static int StartLinuxDirect<T>(this T builder, string[] args, IOutputBackend backend)
        where T : AppBuilderBase<T>, new()
    {
        var lifetime = LinuxFramebufferPlatform.Initialize(builder, backend);
        builder.SetupWithLifetime(lifetime);
        lifetime.Start(args);
        builder.Instance.Run(lifetime.Token);
        return lifetime.ExitCode;
    }

    public static DrmOutput CreateDrmOutput<T>(this T builder, string path = null, bool connectorsForceProbe = false,
        [CanBeNull] DrmOutputOptions options = null)
        where T : AppBuilderBase<T>, new() => CreateDrmOutput(path, connectorsForceProbe, options);
    
    private static DrmOutput CreateDrmOutput(string path = null, bool connectorsForceProbe = false,
        [CanBeNull] DrmOutputOptions options = null)
    {
        DrmCard card = null;
        DrmResources resources = null;
        
        if (path != null)
        {
            if (TryCreateDrmOutputForCard(path, out var drmCard, out var drmResources, connectorsForceProbe))
            {
                card = drmCard;
                resources = drmResources;
            }
        }
        else
        {
            var files = Directory.GetFiles("/dev/dri/")
                .Where(w => Regex.Match(w, "card[0-9]+").Success)
                .OrderBy(o => o);
            foreach(var file in files) 
            {
                if (TryCreateDrmOutputForCard(path, out var drmCard, out var drmResources, connectorsForceProbe))
                {
                    card = drmCard;
                    resources = drmResources;
                    break;
                } 
            }
        }

        if (card == null || resources == null)
            throw new InvalidOperationException("Unable to find connected DRM connector");
        
        var connector =
            resources.Connectors.FirstOrDefault(x => x.Connection == DrmModeConnection.DRM_MODE_CONNECTED);
        if(connector == null)
            throw new InvalidOperationException("Unable to find connected DRM connector");
        
        var mode = connector.Modes.OrderByDescending(x => x.IsPreferred)
            .ThenByDescending(x => x.Resolution.Width * x.Resolution.Height)
            .FirstOrDefault();
        if(mode == null)
            throw new InvalidOperationException("Unable to find a usable DRM mode");
        return new DrmOutput(card, resources, connector, mode);
    }
    
    private static bool TryCreateDrmOutputForCard(string path, [CanBeNull] out DrmCard drmCard, [CanBeNull] out DrmResources drmResources, 
        bool connectorsForceProbe = false)
    {
        if (!DrmCard.TryCreate(path, out var card))
        {
            drmCard = null;
            drmResources = null;
            return false;
        }

        if (!card!.TryGetResources(out var resources, connectorsForceProbe))
        {
            drmCard = null;
            drmResources = null;
            return false;
        }

        drmCard = card;
        drmResources = resources;
        return true;
    }
}

