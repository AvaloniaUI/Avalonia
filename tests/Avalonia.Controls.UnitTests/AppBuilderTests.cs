using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Avalonia.Controls.UnitTests;
using Avalonia.Platform;
using Moq;
using System.Reflection;

[assembly: ExportAvaloniaModule("DefaultModule", typeof(AppBuilderTests.DefaultModule))]
[assembly: ExportAvaloniaModule("RenderingModule", typeof(AppBuilderTests.Direct2DModule), ForRenderingSubsystem = "Direct2D1")]
[assembly: ExportAvaloniaModule("RenderingModule", typeof(AppBuilderTests.SkiaModule), ForRenderingSubsystem = "Skia")]
[assembly: ExportAvaloniaModule("RenderingModule", typeof(AppBuilderTests.DefaultRenderingModule))]
[assembly: ExportAvaloniaModule("OSModule", typeof(AppBuilderTests.WindowsModule), ForOperatingSystem = OperatingSystemType.WinNT)]


namespace Avalonia.Controls.UnitTests
{

    public class AppBuilderTests
    {
        class TestAppBuilder : AppBuilderBase<TestAppBuilder>
        {
            public TestAppBuilder()
                :base(null, builder => { })
            {
            }

            public TestAppBuilder(IRuntimePlatform platform, App app)
                : base(platform,
                      builder => AvaloniaLocator.CurrentMutable.Bind<IRuntimePlatform>().ToConstant(platform))
            {
                Instance = app;
            }
        }

        class MockRuntimePlatform : IRuntimePlatform
        {
            private OperatingSystemType os;

            public MockRuntimePlatform(OperatingSystemType os)
            {
                this.os = os;
            }
            public Assembly[] GetLoadedAssemblies()
            {
                return new[] { typeof(MockRuntimePlatform).Assembly };
            }

            public RuntimePlatformInfo GetRuntimeInfo()
            {
                return new RuntimePlatformInfo { OperatingSystem = os };
            }

            public string GetStackTrace()
            {
                return "";
            }

            public void PostThreadPoolItem(Action cb)
            {
                throw new NotImplementedException();
            }

            public IDisposable StartSystemTimer(TimeSpan interval, Action tick)
            {
                throw new NotImplementedException();
            }
        }

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

        public class WindowsModule
        {
            public static bool IsLoaded = false;
            public WindowsModule()
            {
                IsLoaded = true;
            }
        }
        
        [Fact]
        public void LoadsDefaultModule()
        {
            using (AvaloniaLocator.EnterScope())
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
            using (AvaloniaLocator.EnterScope())
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
            using (AvaloniaLocator.EnterScope())
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

        [Fact]
        public void LoadsModuleAssociatedWithOperatingSystem()
        {
            using (AvaloniaLocator.EnterScope())
            {
                ResetModuleLoadStates();
                var builder = new TestAppBuilder(new MockRuntimePlatform(OperatingSystemType.WinNT), new App());
                builder
                    .UseWindowingSubsystem(() => { })
                    .UseRenderingSubsystem(() => { })
                    .UseAvaloniaModules()
                    .SetupWithoutStarting();
                Assert.True(WindowsModule.IsLoaded);
            }
        }

        [Fact]
        public void DoesNotLoadModuleAssociatedWithDifferentOperatingSystem()
        {
            using (AvaloniaLocator.EnterScope())
            {
                ResetModuleLoadStates();
                var builder = new TestAppBuilder(new MockRuntimePlatform(OperatingSystemType.Android), new App());
                builder
                    .UseWindowingSubsystem(() => { })
                    .UseRenderingSubsystem(() => { })
                    .UseAvaloniaModules()
                    .SetupWithoutStarting();
                Assert.False(WindowsModule.IsLoaded);
            }
        }

        private static void ResetModuleLoadStates()
        {
            DefaultModule.IsLoaded = false;
            DefaultRenderingModule.IsLoaded = false;
            Direct2DModule.IsLoaded = false;
            SkiaModule.IsLoaded = false;
            WindowsModule.IsLoaded = false;
        }
    }
}
