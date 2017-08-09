using System;
using Xunit;
using Avalonia.Controls.UnitTests;
using Avalonia.Platform;
using Avalonia.UnitTests;
using System.Reflection;

[assembly: ExportAvaloniaModule("DefaultModule", typeof(AppBuilderTests.DefaultModule))]
[assembly: ExportAvaloniaModule("RenderingModule", typeof(AppBuilderTests.Direct2DModule), ForRenderingSubsystem = "Direct2D1")]
[assembly: ExportAvaloniaModule("RenderingModule", typeof(AppBuilderTests.SkiaModule), ForRenderingSubsystem = "Skia")]
[assembly: ExportAvaloniaModule("RenderingModule", typeof(AppBuilderTests.DefaultRenderingModule))]


namespace Avalonia.Controls.UnitTests
{
    public class AppBuilderTests
    {
        class App : Application
        {
        }

        public class DefaultModule
        {
            public static bool IsLoaded = false;
            public DefaultModule()
            {
                IsLoaded = true;
            }
        }

        public class DefaultRenderingModule
        {
            public static bool IsLoaded = false;
            public DefaultRenderingModule()
            {
                IsLoaded = true;
            }
        }

        public class Direct2DModule
        {
            public static bool IsLoaded = false;
            public Direct2DModule()
            {
                IsLoaded = true;
            }
        }

        public class SkiaModule
        {
            public static bool IsLoaded = false;
            public SkiaModule()
            {
                IsLoaded = true;
            }
        }
        
        [Fact]
        public void LoadsDefaultModule()
        {
            using (Start())
            {
                ResetModuleLoadStates();
                AppBuilder.Configure<App>()
                    .UseWindowingSubsystem(() => { })
                    .UseRenderingSubsystem(() => { })
                    .UseAvaloniaModules()
                    .SetupWithoutStarting();

                Assert.True(DefaultModule.IsLoaded); 
            }
        }

        [Fact]
        public void LoadsRenderingModuleWithMatchingRenderingSubsystem()
        {
            using (Start())
            {
                ResetModuleLoadStates();
                var builder = AppBuilder.Configure<App>()
                    .UseWindowingSubsystem(() => { })
                    .UseRenderingSubsystem(() => { }, "Direct2D1");
                builder.UseAvaloniaModules().SetupWithoutStarting();
                Assert.False(DefaultRenderingModule.IsLoaded);
                Assert.True(Direct2DModule.IsLoaded);
                Assert.False(SkiaModule.IsLoaded);

                ResetModuleLoadStates();
                builder = AppBuilder.Configure<App>()
                    .UseWindowingSubsystem(() => { })
                    .UseRenderingSubsystem(() => { }, "Skia");
                builder.UseAvaloniaModules().SetupWithoutStarting();
                Assert.False(DefaultRenderingModule.IsLoaded);
                Assert.False(Direct2DModule.IsLoaded);
                Assert.True(SkiaModule.IsLoaded); 
            }
        }

        [Fact]
        public void LoadsRenderingModuleWithoutDependenciesWhenNoModuleMatches()
        {
            using (Start())
            {
                ResetModuleLoadStates();
                var builder = AppBuilder.Configure<App>()
                    .UseWindowingSubsystem(() => { })
                    .UseRenderingSubsystem(() => { }, "Cairo");
                builder.UseAvaloniaModules().SetupWithoutStarting();
                Assert.True(DefaultRenderingModule.IsLoaded);
                Assert.False(Direct2DModule.IsLoaded);
                Assert.False(SkiaModule.IsLoaded); 
            }
        }

        private IDisposable Start()
        {
            var assemblies = new[] { GetType().GetTypeInfo().Assembly };
            var platform = new MockRuntimePlatform(assemblies);
            return UnitTestApplication.Start(new TestServices(platform: platform));
        }

        private static void ResetModuleLoadStates()
        {
            DefaultModule.IsLoaded = false;
            DefaultRenderingModule.IsLoaded = false;
            Direct2DModule.IsLoaded = false;
            SkiaModule.IsLoaded = false;
        }

        class AppBuilder : AppBuilderBase<AppBuilder>
        {
            public AppBuilder()
                : base(new MockRuntimePlatform(), _ => { })
            {
            }
        }
    }
}
