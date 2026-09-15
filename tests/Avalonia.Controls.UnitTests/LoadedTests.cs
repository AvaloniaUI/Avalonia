using System;
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
    public void Loaded_Exception_Does_Not_Prevent_Other_Controls_From_Loading()
    {
        // Some other tests are populating the queue and are not resetting the dispatcher, so we need to purge it
        Control.ResetLoadedQueueForUnitTests();
        using (UnitTestApplication.Start(TestServices.StyledWindow))
        {
            var window = new Window();
            window.Show();
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded, TestContext.Current.CancellationToken);

            // Batch 1: a control whose Loaded handler throws, plus siblings queued in the same batch.
            var throwing = new Button();
            throwing.Loaded += (_, _) => throw new InvalidOperationException("Loaded handler failure");

            var sibling1 = new Button();
            var sibling2 = new Button();
            int sibling1LoadedCount = 0, sibling2LoadedCount = 0;
            sibling1.Loaded += (_, _) => sibling1LoadedCount++;
            sibling2.Loaded += (_, _) => sibling2LoadedCount++;

            window.Content = new StackPanel { Children = { sibling1, throwing, sibling2 } };

            // The exception must surface (propagate to the dispatcher), not be swallowed.
            PumpLoadedJobs(swallowedExceptionsAllowed: 1);

            // Both siblings from the same batch must still have been loaded.
            Assert.True(sibling1.IsLoaded);
            Assert.True(sibling2.IsLoaded);
            Assert.Equal(1, sibling1LoadedCount);
            Assert.Equal(1, sibling2LoadedCount);

            // Batch 2: controls loaded afterwards must still receive Loaded.
            var later = new Button();
            int laterLoadedCount = 0;
            later.Loaded += (_, _) => laterLoadedCount++;
            ((StackPanel)window.Content!).Children.Add(later);

            PumpLoadedJobs(swallowedExceptionsAllowed: 0);

            Assert.True(later.IsLoaded);
            Assert.Equal(1, laterLoadedCount);
        }
    }

    private static void PumpLoadedJobs(int swallowedExceptionsAllowed)
    {
        // A throwing Loaded handler propagates its exception out of the dispatcher job.
        // Keep pumping so that any rescheduled loaded-processing jobs also run, but fail
        // if more exceptions escape than the single expected one.
        for (var i = 0; i <= swallowedExceptionsAllowed; i++)
        {
            try
            {
                Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded, TestContext.Current.CancellationToken);
                return;
            }
            catch (InvalidOperationException) when (i < swallowedExceptionsAllowed)
            {
                // Expected exception from the throwing Loaded handler; continue pumping.
            }
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
