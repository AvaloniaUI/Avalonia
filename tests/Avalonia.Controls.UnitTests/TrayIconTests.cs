using System.Collections.Generic;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests;

public class TrayIconTests : ScopedTestBase
{
    [Fact]
    public void Platform_Impl_Should_Be_Created_Only_After_Icon_Is_Attached_To_Application()
    {
        var impl = new Mock<ITrayIconImpl>();
        var createCount = 0;
        var platform = new MockWindowingPlatform(trayIconImpl: () =>
        {
            createCount++;
            return impl.Object;
        });

        using (UnitTestApplication.Start(new TestServices(windowingPlatform: platform)))
        {
            var target = new TrayIcon { ToolTipText = "Test icon" };
            var icons = new TrayIcons { target };

            Assert.Equal(0, createCount);

            TrayIcon.SetIcons(UnitTestApplication.Current, icons);

            Assert.Equal(1, createCount);
            impl.Verify(x => x.SetToolTipText("Test icon"), Times.Once);
            impl.Verify(x => x.SetIsVisible(true), Times.Once);

            TrayIcon.SetIcons(UnitTestApplication.Current, null);

            impl.Verify(x => x.Dispose(), Times.Once);
        }
    }

    [Fact]
    public void Collection_Changes_Should_Attach_And_Detach_Icons()
    {
        var implementations = new List<Mock<ITrayIconImpl>>();
        var platform = new MockWindowingPlatform(trayIconImpl: () =>
        {
            var impl = new Mock<ITrayIconImpl>();
            implementations.Add(impl);
            return impl.Object;
        });

        using (UnitTestApplication.Start(new TestServices(windowingPlatform: platform)))
        {
            var target = new TrayIcon();
            var icons = new TrayIcons();
            TrayIcon.SetIcons(UnitTestApplication.Current, icons);

            icons.Add(target);

            Assert.Single(implementations);

            icons.Clear();

            implementations[0].Verify(x => x.Dispose(), Times.Once);

            icons.Add(target);

            Assert.Equal(2, implementations.Count);
        }
    }

    [Fact]
    public void Replaced_Collection_Should_No_Longer_Attach_Icons()
    {
        var createCount = 0;
        var platform = new MockWindowingPlatform(trayIconImpl: () =>
        {
            ++createCount;
            return new Mock<ITrayIconImpl>().Object;
        });

        using (UnitTestApplication.Start(new TestServices(windowingPlatform: platform)))
        {
            var oldIcons = new TrayIcons();
            var newIcons = new TrayIcons();

            TrayIcon.SetIcons(UnitTestApplication.Current, oldIcons);
            TrayIcon.SetIcons(UnitTestApplication.Current, newIcons);
            oldIcons.Add(new TrayIcon());

            Assert.Equal(0, createCount);

            newIcons.Add(new TrayIcon());

            Assert.Equal(1, createCount);
        }
    }
}
