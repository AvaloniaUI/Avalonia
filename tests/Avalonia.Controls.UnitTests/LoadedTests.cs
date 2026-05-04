using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests;

public class LoadedTests : ScopedTestBase
{
    [Fact]
    public void Window_Loads_And_Unloads()
    {
        // Some other tests are populating the queue and are not resetting the dispatcher, so we need to purge it
        Control.ResetLoadedQueueForUnitTests();
        using (UnitTestApplication.Start(TestServices.StyledWindow))
        {
            int loadedCount = 0, unloadedCount = 0;
            var target = new Window();

            target.Loaded += (_, _) => loadedCount++;
            target.Unloaded += (_, _) => unloadedCount++; 
            
            Assert.Equal(0, loadedCount);
            Assert.Equal(0, unloadedCount);
            
            target.Show();
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded, TestContext.Current.CancellationToken);
            Assert.True(target.IsLoaded);

            Assert.Equal(1, loadedCount);
            Assert.Equal(0, unloadedCount);
            
            target.Close();
            
            Assert.Equal(1, loadedCount);
            Assert.Equal(1, unloadedCount);
            Assert.False(target.IsLoaded);
        }
    }
    
    [Fact]
    public void Control_Loads_And_Unloads()
    {
        // Some other tests are populating the queue and are not resetting the dispatcher, so we need to purge it
        Control.ResetLoadedQueueForUnitTests();
        using (UnitTestApplication.Start(TestServices.StyledWindow))
        {
            int loadedCount = 0, unloadedCount = 0;
            var window = new Window();
            window.Show();

            var target = new Button();

            target.Loaded += (_, _) => loadedCount++;
            target.Unloaded += (_, _) => unloadedCount++; 
            
            Assert.Equal(0, loadedCount);
            Assert.Equal(0, unloadedCount);
            
            window.Content = target;
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded, TestContext.Current.CancellationToken);
            Assert.True(target.IsLoaded);

            Assert.Equal(1, loadedCount);
            Assert.Equal(0, unloadedCount);
            
            window.Content = null;
            
            Assert.Equal(1, loadedCount);
            Assert.Equal(1, unloadedCount);
            Assert.False(target.IsLoaded);
        }
    }

    [Fact]
    public void Loaded_Should_Not_Be_Raised_If_Detached_From_Visual_Tree()
    {
        using var app = UnitTestApplication.Start(TestServices.StyledWindow);

        var loadedCount = 0;
        var unloadedCount = 0;
        var window = new Window();
        window.Show();

        var target = new Button();

        target.Loaded += (_, _) => loadedCount++;
        target.Unloaded += (_, _) => unloadedCount++;

        Assert.Equal(0, loadedCount);
        Assert.Equal(0, unloadedCount);

        // Attach to, then immediately detach from the visual tree.
        window.Content = target;
        window.Content = null;

        // Attach to another logical parent (this can actually happen outside tests with overlay popups)
        ((ISetLogicalParent) target).SetParent(new Window());

        Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded, TestContext.Current.CancellationToken);

        // At this point, the control shouldn't have been loaded at all.
        Assert.Null(target.VisualParent);
        Assert.False(target.IsLoaded);
        Assert.Equal(0, loadedCount);
        Assert.Equal(0, unloadedCount);
    }
}
