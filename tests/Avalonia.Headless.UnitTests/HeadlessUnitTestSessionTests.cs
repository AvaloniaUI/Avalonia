#if NUNIT
using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
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

#if PERTEST
    // PerTest only: a shared PerAssembly session keeps its services in the root locator, which other sessions inherit.
    [Test]
    public async Task Sessions_On_Different_Threads_Should_Render_Predefined_TextDecorations()
    {
        await using var first = HeadlessUnitTestSession.StartNew(typeof(TestApplication));
        await using var second = HeadlessUnitTestSession.StartNew(typeof(TestApplication));

        // Both sessions are alive at the same time, so their dispatch loops run on different threads.
        var firstThreadId = await first.Dispatch(RenderUnderlinedText, CancellationToken.None)
            .WaitAsync(TimeSpan.FromSeconds(10));
        var secondThreadId = await second.Dispatch(RenderUnderlinedText, CancellationToken.None)
            .WaitAsync(TimeSpan.FromSeconds(10));

        Assert.That(secondThreadId, Is.Not.EqualTo(firstThreadId));

        static int RenderUnderlinedText()
        {
            var window = new Window
            {
                Content = new TextBlock { Text = "Hello, World!", TextDecorations = TextDecorations.Underline },
                Width = 100,
                Height = 100
            };

            window.Show();

            AssertHelper.NotNull(window.CaptureRenderedFrame());

            return Environment.CurrentManagedThreadId;
        }
    }
#endif
}
#endif
