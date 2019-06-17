using System;
using System.Diagnostics;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Controls.Embedding;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.LinuxFramebuffer;
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
        public static FramebufferToplevelImpl TopLevel;
        LinuxFramebufferPlatform(string fbdev = null)
        {
            _fb = new LinuxFramebuffer(fbdev);
        }


        void Initialize()
        {
            Threading = new InternalPlatformThreadingInterface();
            AvaloniaLocator.CurrentMutable
                .Bind<IStandardCursorFactory>().ToTransient<CursorFactoryStub>()
                .Bind<IPlatformSettings>().ToSingleton<PlatformSettings>()
                .Bind<IPlatformThreadingInterface>().ToConstant(Threading)
                .Bind<IRenderLoop>().ToConstant(new RenderLoop())
                .Bind<PlatformHotkeyConfiguration>().ToSingleton<PlatformHotkeyConfiguration>()
                .Bind<IRenderTimer>().ToConstant(Threading);
        }

        internal static TopLevel Initialize<T>(T builder, string fbdev = null) where T : AppBuilderBase<T>, new()
        {
            var platform = new LinuxFramebufferPlatform(fbdev);
            builder.UseSkia().UseWindowingSubsystem(platform.Initialize, "fbdev")
                .SetupWithoutStarting();
            var tl = new EmbeddableControlRoot(TopLevel = new FramebufferToplevelImpl(platform._fb));
            tl.Prepare();
            return tl;
        }
    }
}

public static class LinuxFramebufferPlatformExtensions
{
    class TokenClosable : ICloseable
    {
        public event EventHandler Closed;

        public TokenClosable(CancellationToken token)
        {
            token.Register(() => Dispatcher.UIThread.Post(() => Closed?.Invoke(this, new EventArgs())));
        }
    }

    public static void InitializeWithLinuxFramebuffer<T>(this T builder, Action<TopLevel> setup,
        CancellationToken stop = default(CancellationToken), string fbdev = null)
        where T : AppBuilderBase<T>, new()
    {
        setup(LinuxFramebufferPlatform.Initialize(builder, fbdev));
        builder.BeforeStartCallback(builder);
        builder.Instance.Run(new TokenClosable(stop));
    }
}

