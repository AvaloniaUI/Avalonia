using System;
using System.Diagnostics;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Embedding;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.LinuxFramebuffer;
using Avalonia.LinuxFramebuffer.Input.LibInput;
using Avalonia.LinuxFramebuffer.Input;
using Avalonia.LinuxFramebuffer.Output;
using Avalonia.OpenGL;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Threading;

namespace Avalonia.LinuxFramebuffer
{
    class LinuxFramebufferPlatform
    {
        private IInputBackend _input;
        private IOutputBackend _output;
        private static readonly Stopwatch St = Stopwatch.StartNew();
        internal static uint Timestamp => (uint)St.ElapsedTicks;
        public static InternalPlatformThreadingInterface Threading;
        private Lazy<KeyboardDevice> _keyboardDevice = new Lazy<KeyboardDevice>(() => new KeyboardDevice());
        private Lazy<MouseDevice> _mouseDevice = new Lazy<MouseDevice>(() => new MouseDevice());
        private Lazy<TouchDevice> _touchDevice = new Lazy<TouchDevice>(() => new TouchDevice());
        public KeyboardDevice AKeyboardDevice => _keyboardDevice.Value;
        public MouseDevice AMouseDevice => _mouseDevice.Value;
        public TouchDevice ATouchDevice => _touchDevice.Value;
        LinuxFramebufferPlatform(IInputBackend input, IOutputBackend output)
        {
            _input = input;
            _input.SetMouse(AMouseDevice);
            _input.SetTouch(ATouchDevice);
            _input.SetKeyboard(AKeyboardDevice);
            _output = output;
        }


        void Initialize()
        {
            Threading = new InternalPlatformThreadingInterface();
            if (_output is IWindowingPlatformGlFeature glFeature)
                AvaloniaLocator.CurrentMutable.Bind<IWindowingPlatformGlFeature>().ToConstant(glFeature);
            AvaloniaLocator.CurrentMutable
                .Bind<IPlatformThreadingInterface>().ToConstant(Threading)
                .Bind<IRenderTimer>().ToConstant(new DefaultRenderTimer(60))
                .Bind<IRenderLoop>().ToConstant(new RenderLoop())
                .Bind<IStandardCursorFactory>().ToTransient<CursorFactoryStub>()
                .Bind<IKeyboardDevice>().ToFunc(() => AKeyboardDevice)
                .Bind<TouchDevice>().ToFunc(() => ATouchDevice)
                .Bind<IMouseDevice>().ToFunc(() => AMouseDevice)
                .Bind<IPlatformSettings>().ToSingleton<PlatformSettings>()
                .Bind<IRenderLoop>().ToConstant(new RenderLoop())
                .Bind<PlatformHotkeyConfiguration>().ToSingleton<PlatformHotkeyConfiguration>();

        }

       
        internal static LinuxFramebufferLifetime Initialize<T>(T builder, IInputBackend inputBackend, IOutputBackend outputBackend) where T : AppBuilderBase<T>, new()
        {
            var platform = new LinuxFramebufferPlatform(inputBackend, outputBackend);
            builder.UseSkia().UseWindowingSubsystem(platform.Initialize, "fbdev");
            return new LinuxFramebufferLifetime(platform._input, platform._output);
        }
    }

    class LinuxFramebufferLifetime : IControlledApplicationLifetime, ISingleViewApplicationLifetime
    {

        private readonly IInputBackend _input;
        private readonly IOutputBackend _output;
        private TopLevel _topLevel;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        public CancellationToken Token => _cts.Token;

        public LinuxFramebufferLifetime(IInputBackend input, IOutputBackend output)
        {
            _input = input;
            _output = output;
        }
        
        public Control MainView
        {
            get => (Control)_topLevel?.Content;
            set
            {
                if (_topLevel == null)
                {
                    var tl = new EmbeddableControlRoot(new FramebufferToplevelImpl(_output,_input));
                    tl.Prepare();
                    // Get that keyboard focus going...
                    FocusManager.Instance.SetFocusScope(tl);
                    _topLevel = tl;
                    _topLevel.Renderer.Start();
                    //_topLevel.Renderer.DrawFps = true;
                    //_topLevel.Renderer.DrawDirtyRects = true;
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
    public static int StartLinuxFbDev<T>(this T builder, string[] args, string fbdev = null)
        where T : AppBuilderBase<T>, new() => StartLinuxDirect(builder, args,new LibInputBackend(), new FbdevOutput(fbdev));

    public static int StartLinuxDrm<T>(this T builder, string[] args, string card = null)
        where T : AppBuilderBase<T>, new() => StartLinuxDirect(builder, args,new LibInputBackend(), new DrmOutput(card));
    
    public static int StartLinuxDirect<T>(this T builder, string[] args, IInputBackend input, IOutputBackend backend)
        where T : AppBuilderBase<T>, new()
    {
        var lifetime = LinuxFramebufferPlatform.Initialize(builder, input, backend);
        builder.Instance.ApplicationLifetime = lifetime;
        builder.SetupWithoutStarting();
        lifetime.Start(args);
        builder.Instance.Run(lifetime.Token);
        return lifetime.ExitCode;
    }
}

