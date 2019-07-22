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
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Threading;

namespace Avalonia.LinuxFramebuffer
{
    class LinuxFramebufferPlatform
    {
        LinuxFramebuffer _fb;
        public static KeyboardDevice KeyboardDevice = new KeyboardDevice();
        public static MouseDevice MouseDevice = new MouseDevice();
        private static readonly Stopwatch St = Stopwatch.StartNew();
        internal static uint Timestamp => (uint)St.ElapsedTicks;
        public static InternalPlatformThreadingInterface Threading;
        LinuxFramebufferPlatform(string fbdev = null)
        {
            _fb = new LinuxFramebuffer(fbdev);
        }


        void Initialize()
        {
            Threading = new InternalPlatformThreadingInterface();
            AvaloniaLocator.CurrentMutable
                .Bind<IStandardCursorFactory>().ToTransient<CursorFactoryStub>()
                .Bind<IKeyboardDevice>().ToConstant(KeyboardDevice)
                .Bind<IPlatformSettings>().ToSingleton<PlatformSettings>()
                .Bind<IPlatformThreadingInterface>().ToConstant(Threading)
                .Bind<IRenderLoop>().ToConstant(new RenderLoop())
                .Bind<PlatformHotkeyConfiguration>().ToSingleton<PlatformHotkeyConfiguration>()
                .Bind<IRenderTimer>().ToConstant(Threading);
        }

        internal static LinuxFramebufferLifetime Initialize<T>(T builder, string fbdev = null) where T : AppBuilderBase<T>, new()
        {
            var platform = new LinuxFramebufferPlatform(fbdev);
            builder.UseSkia().UseWindowingSubsystem(platform.Initialize, "fbdev");
            return new LinuxFramebufferLifetime(platform._fb);
        }
    }

    class LinuxFramebufferLifetime : IControlledApplicationLifetime, ISingleViewApplicationLifetime
    {
        private readonly LinuxFramebuffer _fb;
        private TopLevel _topLevel;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        public CancellationToken Token => _cts.Token;

        public LinuxFramebufferLifetime(LinuxFramebuffer fb)
        {
            _fb = fb;
        }
        
        public Control MainView
        {
            get => (Control)_topLevel?.Content;
            set
            {
                if (_topLevel == null)
                {

                    var tl = new EmbeddableControlRoot(new FramebufferToplevelImpl(_fb, new LibInputBackend()));
                    tl.Prepare();
                    _topLevel = tl;
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
    public static int StartLinuxFramebuffer<T>(this T builder, string[] args, string fbdev = null)
        where T : AppBuilderBase<T>, new()
    {
        var lifetime = LinuxFramebufferPlatform.Initialize(builder, fbdev);
        builder.Instance.ApplicationLifetime = lifetime;
        builder.SetupWithoutStarting();
        lifetime.Start(args);
        builder.Instance.Run(lifetime.Token);
        return lifetime.ExitCode;
    }
}

