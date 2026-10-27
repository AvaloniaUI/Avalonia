using System.Reflection;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Xunit;
using Xunit.Sdk;

namespace Avalonia.Base.UnitTests;

public class VerifyEmptyDispatcherAfterTestAttributeTests : ScopedTestBase
{
    private static MethodInfo UnscopedTest { get; } =
        typeof(UnscopedTests).GetMethod(nameof(UnscopedTests.Test))!;

    [Fact]
    public void After_Fails_An_Unscoped_Test_That_Leaves_A_UI_Thread_Dispatcher()
    {
        _ = Dispatcher.UIThread;

        var attribute = new VerifyEmptyDispatcherAfterTestAttribute();

        Assert.Throws<FailException>(() => attribute.After(UnscopedTest, null!));

        // The leaked dispatcher is released, so only the leaking test fails.
        Assert.Null(Dispatcher.TryGetUIThread());
    }

    [Fact]
    public void After_Passes_An_Unscoped_Test_That_Leaves_No_UI_Thread_Dispatcher()
    {
        Dispatcher.ResetBeforeUnitTests();

        var attribute = new VerifyEmptyDispatcherAfterTestAttribute();

        attribute.After(UnscopedTest, null!);

        Assert.Null(Dispatcher.TryGetUIThread());
    }

    private class UnscopedTests
    {
        public void Test()
        {
        }
    }
}
