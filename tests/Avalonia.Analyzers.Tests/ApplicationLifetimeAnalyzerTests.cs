using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Avalonia.Analyzers.Tests.GeneratedProperties;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Avalonia.Analyzers.Tests;

public class ApplicationLifetimeAnalyzerTests
{
    private static Task Verify([StringSyntax("csharp")] string source)
    {
        var test = new ApplicationLifetimeAnalyzerTest
        {
            TestCode = """
                       #nullable enable
                       using System;
                       using Avalonia;
                       using Avalonia.Controls;
                       using Avalonia.Controls.ApplicationLifetimes;

                       """ + source
        };

        return test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public Task Hard_Cast_Outside_App_Reports_AVA2003() => Verify(
        """
        public class ViewModel
        {
            public void Run()
            {
                var lifetime = {|AVA2003:(IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!|};
            }
        }
        """);

    [Fact]
    public Task Hard_Cast_Inside_App_Reports_AVA2003() => Verify(
        """
        public class App : Application
        {
            public override void OnFrameworkInitializationCompleted()
            {
                var lifetime = {|AVA2003:(IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!|};
            }
        }
        """);

    [Fact]
    public Task Hard_Cast_With_Conditional_Access_Reports_AVA2003() => Verify(
        """
        public class ViewModel
        {
            public void Run()
            {
                var lifetime = {|AVA2003:(ISingleViewApplicationLifetime?)Application.Current?.ApplicationLifetime|};
            }
        }
        """);

    [Fact]
    public Task As_Cast_With_Null_Suppression_Reports_AVA2003() => Verify(
        """
        public class ViewModel
        {
            public void Run()
            {
                ({|AVA2003:Application.Current!.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime|})!.Shutdown();
            }
        }
        """);

    [Fact]
    public Task Hard_Cast_And_MainWindow_Read_Report_AVA2003_And_AVA2004() => Verify(
        """
        public class ViewModel
        {
            public Window? Owner => ({|AVA2003:(IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!|}).{|AVA2004:MainWindow|};
        }
        """);

    [Fact]
    public Task Checked_Casts_Do_Not_Report() => Verify(
        """
        public class DialogService
        {
            public void Run()
            {
                (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown();

                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    desktop.Shutdown();
                }
            }
        }
        """);

    [Fact]
    public Task MainWindow_Read_Inside_App_Does_Not_Report() => Verify(
        """
        public class App : Application
        {
            public override void OnFrameworkInitializationCompleted()
            {
                if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    desktop.MainWindow = new Window();
                    desktop.Startup += (_, _) => desktop.MainWindow?.Activate();
                }
            }
        }
        """);

    [Fact]
    public Task MainWindow_Read_Outside_App_Reports_AVA2004() => Verify(
        """
        public class DialogService
        {
            public Window? GetOwner(IClassicDesktopStyleApplicationLifetime desktop) => desktop.{|AVA2004:MainWindow|};
        }
        """);

    [Fact]
    public Task MainWindow_Conditional_Read_Reports_AVA2004() => Verify(
        """
        public class DialogService
        {
            public Window? GetOwner(IClassicDesktopStyleApplicationLifetime? desktop) => desktop?.{|AVA2004:MainWindow|};
        }
        """);

    [Fact]
    public Task MainWindow_Read_From_Derived_Lifetime_Reports_AVA2004() => Verify(
        """
        public class MyLifetime : ClassicDesktopStyleApplicationLifetime
        {
            public Window? Owner => {|AVA2004:MainWindow|};
        }
        """);

    [Fact]
    public Task MainWindow_Property_Pattern_Reports_AVA2004() => Verify(
        """
        public class DialogService
        {
            public Window? GetOwner() =>
                Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { {|AVA2004:MainWindow|}: { } window }
                    ? window
                    : null;
        }
        """);

    [Fact]
    public Task MainWindow_Write_And_Windows_Read_Do_Not_Report() => Verify(
        """
        public class Setup
        {
            public string Run(IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new Window();
                var count = desktop.Windows.Count;
                return nameof(IClassicDesktopStyleApplicationLifetime.MainWindow);
            }
        }
        """);

    private class ApplicationLifetimeAnalyzerTest : CSharpAnalyzerTest<ApplicationLifetimeAnalyzer, DefaultVerifier>
    {
        public ApplicationLifetimeAnalyzerTest()
        {
            ReferenceAssemblies = new ReferenceAssemblies(TestReferences.DefaultTargetFramework);
            foreach (var reference in TestReferences.All.Value)
            {
                TestState.AdditionalReferences.Add(reference);
            }

            TestState.AdditionalReferences.Add(MetadataReference.CreateFromFile(typeof(Application).Assembly.Location));
        }
    }
}
