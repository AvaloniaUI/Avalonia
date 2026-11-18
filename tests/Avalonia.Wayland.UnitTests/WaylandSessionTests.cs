using System;
using Avalonia;
using Xunit;

namespace Avalonia.Wayland.UnitTests;

public class WaylandSessionTests
{
    [Fact]
    public void Session_Management_Is_Off_By_Default()
    {
        var options = new WaylandPlatformOptions();

        Assert.False(options.UseSessionManagement);
        Assert.Null(options.SessionId);
        Assert.Null(options.SessionCreated);
    }

    [Fact]
    public void Window_Constructed_Outside_A_Name_Scope_Has_No_Name()
    {
        Assert.Null(WaylandSession.Take());
    }

    [Fact]
    public void Name_Is_Taken_By_The_Next_Window_Only()
    {
        using (WaylandSession.Name("main"))
        {
            Assert.Equal("main", WaylandSession.Take());
            Assert.Null(WaylandSession.Take());
        }
    }

    [Fact]
    public void Disposing_A_Scope_Restores_The_Enclosing_Name()
    {
        using (WaylandSession.Name("outer"))
        {
            using (WaylandSession.Name("inner"))
                Assert.Equal("inner", WaylandSession.Take());

            Assert.Equal("outer", WaylandSession.Take());
        }

        Assert.Null(WaylandSession.Take());
    }

    [Fact]
    public void Name_Is_Not_Seen_By_Other_Threads()
    {
        using (WaylandSession.Name("main"))
        {
            string? other = "unset";
            var thread = new System.Threading.Thread(() => other = WaylandSession.Take());
            thread.Start();
            thread.Join();

            Assert.Null(other);
            Assert.Equal("main", WaylandSession.Take());
        }
    }

    [Fact]
    public void Null_Name_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => WaylandSession.Name(null!));
    }
}
