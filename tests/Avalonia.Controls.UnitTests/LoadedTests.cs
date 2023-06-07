using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests;

public class LoadedTests
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
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);
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
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);
            Assert.True(target.IsLoaded);

            Assert.Equal(1, loadedCount);
            Assert.Equal(0, unloadedCount);
            
            window.Content = null;
            
            Assert.Equal(1, loadedCount);
            Assert.Equal(1, unloadedCount);
            Assert.False(target.IsLoaded);
        }
    }
}
