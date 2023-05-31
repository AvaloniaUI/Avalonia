using Avalonia.Compatibility;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Dialogs;
using Avalonia.Headless;
using Avalonia.Headless.Vnc;
using Avalonia.Platform;

namespace Avalonia
{
    public static class HeadlessVncPlatformExtensions
    {
        /// <summary>
        /// Run a headless VNC session where the client size is determined by the size of the 
        /// <seealso cref="IClassicDesktopStyleApplicationLifetime.MainWindow"/>
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="host">The listening host address, if null defaults to localhost</param>
        /// <param name="port">The port number to lisen on</param>
        /// <param name="args"></param>
        /// <param name="shutdownMode">
        /// If <see cref="ShutdownMode.OnLastWindowClose"/> shutdown will happen when the last
        /// VNC client disconnects
        /// </param>
        /// <param name="frameBufferFormat">
        /// The pixel format to send from Skia to the client. 
        /// Defaults to <seealso cref="PixelFormat.Bgra8888"/>
        /// </param>
        /// <param name="resizeSessionIfContentSizeChanges">
        /// If true then the VNC session will try to resize to match the window size when the window size changes.
        /// This can cause some VNC clients to stop working so use at your own risk.
        /// </param>
        public static int StartWithHeadlessVncPlatform(
            this AppBuilder builder,
            string host, int port,
            string[] args, ShutdownMode shutdownMode = ShutdownMode.OnLastWindowClose,
            PixelFormat? frameBufferFormat = null,
            bool resizeSessionIfContentSizeChanges = false)
        {
            HeadlessVncConnectionManager connManager = new(builder, host, port, shutdownMode, resizeSessionIfContentSizeChanges);

            if (OperatingSystemEx.IsWindows())
                builder.UseWin32MountedVolumeInfoProvider();

            else if (OperatingSystemEx.IsMacOS())
                builder.UseAvaloniaNativeMountedVolumeInfoProvider();

            else if (OperatingSystemEx.IsLinux())
                builder.UseX11MountedVolumeInfoProvider();

            frameBufferFormat ??= PixelFormat.Bgra8888;
            return builder
                .UseHeadless(new AvaloniaHeadlessPlatformOptions
                {
                    UseHeadlessDrawing = false,
                    FrameBufferFormat = frameBufferFormat.Value
                })
                .UseManagedSystemDialogs()
                .AfterPlatformServicesSetup(_ => 
                {
                    AvaloniaLocator.CurrentMutable
                        .Bind<IWindowingPlatform>().ToConstant(new HeadlessVncWindowingPlatform(
                            frameBufferFormat.Value, connManager));
                })
                .StartWithClassicDesktopLifetime(args, shutdownMode);
        }
    }
}
