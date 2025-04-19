using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls.Platform;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests;

// Some of these exceptions are based from https://github.com/dotnet/wpf-test/blob/05797008bb4975ceeb71be36c47f01688f535d53/src/Test/ElementServices/FeatureTests/Untrusted/Dispatcher/UnhandledExceptionTest.cs#L30
public partial class DispatcherTests : ScopedTestBase
{
    private const string ExpectedExceptionText = "Exception thrown inside Dispatcher.Invoke / Dispatcher.BeginInvoke.";

    private int _numberOfHandlerOnUnhandledEventInvoked;
    private int _numberOfHandlerOnUnhandledEventFilterInvoked;
    private Dispatcher _uiThread;

    public DispatcherTests()
    {
        _numberOfHandlerOnUnhandledEventInvoked = 0;
        _numberOfHandlerOnUnhandledEventFilterInvoked = 0;
        
        VerifyDispatcherSanity();
        _uiThread = Dispatcher.CurrentDispatcher;
    }

    void VerifyDispatcherSanity()
    {
        // Verify that we are in a clear-ish state. Do this for every test to ensure that our reset procedure is working
        Assert.Null(Dispatcher.FromThread(Thread.CurrentThread));
        Assert.Null(Dispatcher.TryGetUIThread());
        
        // The first (this) dispatcher becomes UI thread one
        Assert.NotNull(Dispatcher.CurrentDispatcher);
        Assert.Equal(Dispatcher.TryGetUIThread(), Dispatcher.CurrentDispatcher);
        Assert.Equal(Dispatcher.UIThread, Dispatcher.CurrentDispatcher);
        
        // Dispatcher.FromThread works
        Assert.Equal(Dispatcher.CurrentDispatcher, Dispatcher.FromThread(Thread.CurrentThread));
        Assert.Equal(Dispatcher.UIThread, Dispatcher.FromThread(Thread.CurrentThread));
    }

    [Fact]
    public void Different_Threads_Auto_Spawn_Dispatchers()
    {
        var dispatcher = Dispatcher.CurrentDispatcher;
        ThreadRunHelper.RunOnDedicatedThread(() =>
        {
            Assert.Null(Dispatcher.FromThread(Thread.CurrentThread));
            Assert.NotNull(Dispatcher.CurrentDispatcher);
            Assert.NotEqual(dispatcher, Dispatcher.CurrentDispatcher);
            Assert.Equal(Dispatcher.CurrentDispatcher, Dispatcher.FromThread(Thread.CurrentThread));
        }).GetAwaiter().GetResult();
    }
    
    [Fact]
    public void DispatcherHandlesExceptionWithPost()
    {
        var handled = false;
        var executed = false;
        _uiThread.UnhandledException += (sender, args) =>
        {
            handled = true;
            args.Handled = true;
        };
        _uiThread.Post(() => ThrowAnException());
        _uiThread.Post(() => executed = true);

        _uiThread.RunJobs();
        
        Assert.True(handled);
        Assert.True(executed);
    }

    [Fact]
    public void SyncContextExceptionCanBeHandledWithPost()
    {
        var syncContext = _uiThread.GetContextWithPriority(DispatcherPriority.Background);

        var handled = false;
        var executed = false;
        _uiThread.UnhandledException += (sender, args) =>
        {
            handled = true;
            args.Handled = true;
        };

        syncContext.Post(_ => ThrowAnException(), null);
        syncContext.Post(_ => executed = true, null);

        _uiThread.RunJobs();

        Assert.True(handled);
        Assert.True(executed);
    }

    [Fact]
    public void CanRemoveDispatcherExceptionHandler()
    {
        var caughtCorrectException = false;

        _uiThread.UnhandledExceptionFilter +=
            HandlerOnUnhandledExceptionFilterRequestCatch;
        _uiThread.UnhandledException +=
            HandlerOnUnhandledExceptionNotHandled;

        _uiThread.UnhandledExceptionFilter -=
            HandlerOnUnhandledExceptionFilterRequestCatch;
        _uiThread.UnhandledException -=
            HandlerOnUnhandledExceptionNotHandled;

        try
        {
            _uiThread.Post(ThrowAnException, DispatcherPriority.Normal);
            _uiThread.RunJobs();
        }
        catch (Exception e)
        {
            caughtCorrectException = e.Message == ExpectedExceptionText;
        }
        finally
        {
            Verification(caughtCorrectException, 0, 0);
        }
    }

    [Fact]
    public void CanHandleExceptionWithUnhandledException()
    {
        _uiThread.UnhandledExceptionFilter +=
            HandlerOnUnhandledExceptionFilterRequestCatch;
        
        _uiThread.UnhandledException +=
            HandlerOnUnhandledExceptionHandled;
        var caughtCorrectException = true;
        try
        {
            _uiThread.Post(ThrowAnException, DispatcherPriority.Normal);
            _uiThread.RunJobs();
        }
        catch (Exception)
        {
            // should be no exception here.
            caughtCorrectException = false;
        }
        finally
        {
            Verification(caughtCorrectException, 1, 1);
        }
    }

    [Fact]
    public void InvokeMethodDoesntTriggerUnhandledException()
    {
        _uiThread.UnhandledExceptionFilter +=
            HandlerOnUnhandledExceptionFilterRequestCatch;
        
        _uiThread.UnhandledException +=
            HandlerOnUnhandledExceptionHandled;
        var caughtCorrectException = false;
        try
        {
            // Since both Invoke and InvokeAsync can throw exception, there is no need to pass them to the UnhandledException.
            _uiThread.Invoke(ThrowAnException, DispatcherPriority.Normal);
            _uiThread.RunJobs();
        }
        catch (Exception e)
        {
            // should be no exception here.
            caughtCorrectException = e.Message == ExpectedExceptionText;
        }
        finally
        {
            Verification(caughtCorrectException, 0, 0);
        }
    }

    [Fact]
    public void InvokeAsyncMethodDoesntTriggerUnhandledException()
    {
        _uiThread.UnhandledExceptionFilter +=
            HandlerOnUnhandledExceptionFilterRequestCatch;
        
        _uiThread.UnhandledException +=
            HandlerOnUnhandledExceptionHandled;
        var caughtCorrectException = false;
        try
        {
            // Since both Invoke and InvokeAsync can throw exception, there is no need to pass them to the UnhandledException.
            var op = _uiThread.InvokeAsync(ThrowAnException, DispatcherPriority.Normal);
            op.Wait();
            _uiThread.RunJobs();
        }
        catch (Exception e)
        {
            // should be no exception here.
            caughtCorrectException = e.Message == ExpectedExceptionText;
        }
        finally
        {
            Verification(caughtCorrectException, 0, 0);
        }
    }

    [Fact]
    public void CanRethrowExceptionWithUnhandledException()
    {
        _uiThread.UnhandledExceptionFilter +=
            HandlerOnUnhandledExceptionFilterRequestCatch;
        
        _uiThread.UnhandledException +=
            HandlerOnUnhandledExceptionNotHandled;
        var caughtCorrectException = false;
        try
        {
            _uiThread.Post(ThrowAnException, DispatcherPriority.Normal);
            _uiThread.RunJobs();
        }
        catch (Exception e)
        {
            caughtCorrectException = e.Message == ExpectedExceptionText;
        }
        finally
        {
            Verification(caughtCorrectException, 1, 1);
        }
    }

    [Fact]
    public void MultipleUnhandledExceptionFilterCannotResetRequestCatchFlag()
    {
        _uiThread.UnhandledExceptionFilter +=
            HandlerOnUnhandledExceptionFilterNotRequestCatch;
        _uiThread.UnhandledExceptionFilter +=
            HandlerOnUnhandledExceptionFilterRequestCatch;
        
        _uiThread.UnhandledException +=
            HandlerOnUnhandledExceptionNotHandled;
        _uiThread.UnhandledException +=
            HandlerOnUnhandledExceptionHandled;
        var caughtCorrectException = false;
        try
        {
            _uiThread.Post(ThrowAnException, DispatcherPriority.Normal);
            _uiThread.RunJobs();
        }
        catch (Exception e)
        {
            caughtCorrectException = e.Message == ExpectedExceptionText;
        }
        finally
        {
            Verification(caughtCorrectException, 0, 2);
        }
    }

    [Fact]
    public void MultipleUnhandledExceptionCannotResetHandleFlag()
    {
        _uiThread.UnhandledExceptionFilter +=
            HandlerOnUnhandledExceptionFilterRequestCatch;
        
        _uiThread.UnhandledException +=
            HandlerOnUnhandledExceptionHandled;
        _uiThread.UnhandledException +=
            HandlerOnUnhandledExceptionNotHandled;
        var caughtCorrectException = true;
        
        try
        {
            _uiThread.Post(ThrowAnException, DispatcherPriority.Normal);
            _uiThread.RunJobs();
        }
        catch (Exception)
        {
            // should be no exception here.
            caughtCorrectException = false;
        }
        finally
        {
            Verification(caughtCorrectException, 1, 1);
        }
    }

    [Fact]
    public void CanPushFrameAndShutdownDispatcherFromUnhandledException()
    {
        _uiThread.UnhandledExceptionFilter +=
            HandlerOnUnhandledExceptionFilterNotRequestCatchPushFrame;
        
        _uiThread.UnhandledException +=
            HandlerOnUnhandledExceptionHandledPushFrame;
        var caughtCorrectException = false;
        try
        {
            _uiThread.Post(ThrowAnException, DispatcherPriority.Normal);
            _uiThread.RunJobs();
        }
        catch (Exception e)
        {
            caughtCorrectException = e.Message == ExpectedExceptionText;
        }
        finally
        {
            Verification(caughtCorrectException, 0, 1);
        }
    }
    
    private void Verification(bool caughtCorrectException, int numberOfHandlerOnUnhandledEventShouldInvoke,
        int numberOfHandlerOnUnhandledEventFilterShouldInvoke)
    {
        Assert.True(
            _numberOfHandlerOnUnhandledEventInvoked >= numberOfHandlerOnUnhandledEventShouldInvoke,
            "Number of handler invoked on UnhandledException is invalid");

        Assert.True(
            _numberOfHandlerOnUnhandledEventFilterInvoked >= numberOfHandlerOnUnhandledEventFilterShouldInvoke,
            "Number of handler invoked on UnhandledExceptionFilter is invalid");

        Assert.True(caughtCorrectException, "Wrong exception caught.");
    }

    private void HandlerOnUnhandledExceptionFilterRequestCatch(object sender,
        DispatcherUnhandledExceptionFilterEventArgs args)
    {
        args.RequestCatch = true;

        _numberOfHandlerOnUnhandledEventFilterInvoked += 1;
        Assert.Equal(ExpectedExceptionText, args.Exception.Message);
    }

    private void HandlerOnUnhandledExceptionFilterNotRequestCatchPushFrame(object sender,
        DispatcherUnhandledExceptionFilterEventArgs args)
    {
        HandlerOnUnhandledExceptionFilterNotRequestCatch(sender, args);
        var frame = new DispatcherFrame();
        args.Dispatcher.InvokeAsync(() => frame.Continue = false, DispatcherPriority.Background);
        args.Dispatcher.PushFrame(frame);
    }

    private void HandlerOnUnhandledExceptionFilterNotRequestCatch(object sender,
        DispatcherUnhandledExceptionFilterEventArgs args)
    {
        args.RequestCatch = false;
        _numberOfHandlerOnUnhandledEventFilterInvoked += 1;

        Assert.Equal(ExpectedExceptionText, args.Exception.Message);
    }

    private void HandlerOnUnhandledExceptionHandledPushFrame(object sender, DispatcherUnhandledExceptionEventArgs args)
    {
        Assert.Equal(ExpectedExceptionText, args.Exception.Message);
        Assert.False(_numberOfHandlerOnUnhandledEventFilterInvoked == 0,
            "UnhandledExceptionFilter should be invoked before UnhandledException.");

        args.Handled = true;
        _numberOfHandlerOnUnhandledEventInvoked += 1;

        var dispatcher = args.Dispatcher;
        var frame = new DispatcherFrame();
        dispatcher.BeginInvokeShutdown(DispatcherPriority.Background);
        dispatcher.PushFrame(frame);
    }

    private void HandlerOnUnhandledExceptionHandled(object sender, DispatcherUnhandledExceptionEventArgs args)
    {
        Assert.Equal(ExpectedExceptionText, args.Exception.Message);
        Assert.False(_numberOfHandlerOnUnhandledEventFilterInvoked == 0,
            "UnhandledExceptionFilter should be invoked before UnhandledException.");

        args.Handled = true;
        _numberOfHandlerOnUnhandledEventInvoked += 1;
    }

    private void HandlerOnUnhandledExceptionNotHandled(object sender, DispatcherUnhandledExceptionEventArgs args)
    {
        Assert.Equal(ExpectedExceptionText, args.Exception.Message);
        Assert.False(_numberOfHandlerOnUnhandledEventFilterInvoked == 0,
            "UnhandledExceptionFilter should be invoked before UnhandledException.");

        args.Handled = false;
        _numberOfHandlerOnUnhandledEventInvoked += 1;
    }

    private void ThrowAnException()
    {
        throw new Exception(ExpectedExceptionText);
    }
}

