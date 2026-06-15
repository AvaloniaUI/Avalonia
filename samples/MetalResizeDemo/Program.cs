using System;
using System.Linq;
using Avalonia;

namespace MetalResizeDemo
{
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args) => BuildAvaloniaApp(args)
            .StartWithClassicDesktopLifetime(args);

        public static AppBuilder BuildAvaloniaApp() => BuildAvaloniaApp(Array.Empty<string>());

        public static AppBuilder BuildAvaloniaApp(string[] args)
        {
            var builder = AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();

            if (OperatingSystem.IsMacOS())
            {
                var mode = AvaloniaNativeRenderingMode.Metal;
                if (args.Contains("--opengl"))
                    mode = AvaloniaNativeRenderingMode.OpenGl;
                else if (args.Contains("--software"))
                    mode = AvaloniaNativeRenderingMode.Software;

                Console.WriteLine($"MetalResizeDemo: requesting macOS rendering mode '{mode}'.");

                builder = builder.With(new AvaloniaNativePlatformOptions
                {
                    RenderingMode = new[] { mode }
                });
            }

            return builder;
        }
    }
}
