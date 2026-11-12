#if NUNIT
using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace Avalonia.Headless.UnitTests;

public class HeadlessUnitTestSessionTests
{
    [TestCase(false)]
    [TestCase(true)]
    public async Task Session_Should_Support_Immediate_Disposal(bool asynchronously)
    {
        for (var i = 0; i < 10000; ++i)
        {
            var session = HeadlessUnitTestSession.StartNew(typeof(TestApplication));
            if (asynchronously)
                await session.DisposeAsync();
            else
                session.Dispose();
        }
    }

    [Test]
    public async Task Dispatch_Should_Report_Cleanup_Exceptions_And_Continue()
    {
        var session = HeadlessUnitTestSession.GetOrStartForAssembly(GetType().Assembly);

        const string message = "Thrown by a dispatcher job during cleanup.";
        var poisonedDispatch = session.Dispatch(
            () => Dispatcher.UIThread.Post(() => throw new InvalidOperationException(message)),
            CancellationToken.None);

        var exception = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await poisonedDispatch.WaitAsync(TimeSpan.FromSeconds(10)));

        Assert.That(exception!.Message, Is.EqualTo(message));

        var result = await session.Dispatch(() => 42, CancellationToken.None)
            .WaitAsync(TimeSpan.FromSeconds(10));

        Assert.That(result, Is.EqualTo(42));
    }
}
#endif
