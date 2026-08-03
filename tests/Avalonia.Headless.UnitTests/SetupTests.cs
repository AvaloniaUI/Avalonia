using System;

namespace Avalonia.Headless.UnitTests;

public class SetupTests
#if XUNIT
    : IDisposable
#endif
{
    private static int s_instanceCount;

#if NUNIT
    [SetUp]
    public void SetUp()
#elif XUNIT
    public SetupTests()
#endif
    {
        ++s_instanceCount;
    }

#if NUNIT
    [AvaloniaTest, TestCase(1), TestCase(2)]
#elif XUNIT
    [AvaloniaTheory, InlineData(1), InlineData(2)]
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(
        "Usage",
        "xUnit1026:Theory methods should use all of their parameters",
        Justification = "Used to run the test several times")]
#endif
    public void Setup_TearDown_Should_Work(int index)
    {
        AssertHelper.Equal(1, s_instanceCount);
    }

#if NUNIT
    [TearDown]
    public void TearDown()
#elif XUNIT
    public void Dispose()
#endif
    {
        --s_instanceCount;
    }
}
