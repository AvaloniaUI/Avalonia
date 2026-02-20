#if NUNIT

using System.Threading.Tasks;

namespace Avalonia.Headless.UnitTests;

public class AsyncSetupTests
{
    private static int s_instanceCount;

    [SetUp]
    public async Task SetUp()
    {
        await Task.Delay(100);
        ++s_instanceCount;
    }

    [AvaloniaTest, TestCase(1), TestCase(2)]
    public void Async_Setup_TearDown_Should_Work(int index)
    {
        AssertHelper.Equal(1, s_instanceCount);
    }

    [TearDown]
    public async Task TearDown()
    {
        await Task.Delay(100);
        --s_instanceCount;
    }
}

#endif
