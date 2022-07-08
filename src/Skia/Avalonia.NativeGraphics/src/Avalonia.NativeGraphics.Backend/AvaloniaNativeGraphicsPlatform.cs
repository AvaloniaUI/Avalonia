using System;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Native.Interop;
using Avalonia.NativeGraphics.Backend;
using Avalonia.Platform;
using Avalonia.Platform.Interop;
using JetBrains.Annotations;
using MicroCom.Runtime;

namespace Avalonia
{
    public class AvaloniaNativeGraphicsPlatformOptions
    {
        [CanBeNull] public string LibraryPath { get; set; }
    }
    
    public static class AvaloniaNativeGraphicsPlatform
    {
        [DllImport("libAvaloniaNativeGraphics")]
        static extern IntPtr CreateAvaloniaNativeGraphics();

        public static unsafe void Initialize(AvaloniaNativeGraphicsPlatformOptions options)
        {
            IntPtr graphics;
            if (options.LibraryPath != null)
            {
                var loader = AvaloniaLocator.Current.GetService<IDynamicLibraryLoader>()!;
                var lib = loader.LoadLibrary(options.LibraryPath);
                var create =
                    (delegate* unmanaged[Stdcall]<IntPtr>)loader.GetProcAddress(lib, "CreateAvaloniaNativeGraphics",
                        false);
                graphics = create();
            }
            else
                graphics = CreateAvaloniaNativeGraphics();

            var factory = MicroComRuntime.CreateProxyFor<IAvgFactory>(graphics, true);
            
            AvaloniaLocator.CurrentMutable
            .Bind<IPlatformRenderInterface>().ToConstant(new PlatformRenderInterface(factory))
                .Bind<IFontManagerImpl>().ToConstant(new FontManagerStub())
                .Bind<ITextShaperImpl>().ToConstant(new TextShaperStub());
        }

        public static TAppBuilder UseAvaloniaNativeGraphics<TAppBuilder>(this TAppBuilder builder)
            where TAppBuilder : AppBuilderBase<TAppBuilder>, new()
        {
            return builder.UseRenderingSubsystem(() =>
                Initialize(AvaloniaLocator.Current.GetService<AvaloniaNativeGraphicsPlatformOptions>()
                           ?? new AvaloniaNativeGraphicsPlatformOptions()));
        }
    }
}