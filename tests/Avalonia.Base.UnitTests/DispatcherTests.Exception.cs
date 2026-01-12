using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls.Platform;
using Avalonia.Threading;
using Xunit;

namespace Avalonia.Base.UnitTests;

// Some of these exceptions are based from https://github.com/dotnet/wpf-test/blob/05797008bb4975ceeb71be36c47f01688f535d53/src/Test/ElementServices/FeatureTests/Untrusted/Dispatcher/UnhandledExceptionTest.cs#L30
public partial class DispatcherTests
{
    private const string ExpectedExceptionText = "Exception thrown inside Dispatcher.Invoke / Dispatcher.BeginInvoke.";

    private int _numberOfHandlerOnUnhandledEventInvoked;
    private int _numberOfHandlerOnUnhandledEventFilterInvoked;

    public DispatcherTests()
    {
        _numberOfHandlerOnUnhandledEventInvoked = 0;
        _numberOfHandlerOnUnhandledEventFilterInvoked = 0;
    }

    [Fact]
    public void DispatcherHandlesExceptionWithPost()
    {
        var impl = new ManagedDispatcherImpl(null);
        var disp = new Dispatcher(impl);

        var handled = false;
        var executed = false;
        disp.UnhandledException += (sender, args) =>
        {
            handled = true;
            args.Handled = true;
        };
        disp.Post(() => ThrowAnException());
        disp.Post(() => executed = true);

        disp.RunJobs(null, TestContext.Current.CancellationToken);
        
        Assert.True(handled);
        Assert.True(executed);
    }

    [Fact]
    public void SyncContextExceptionCanBeHandledWithPost()
    {
        var impl = new ManagedDispatcherImpl(null);
        var disp = new Dispatcher(impl);

        var syncContext = disp.GetContextWithPriority(DispatcherPriority.Background);

        var handled = false;
        var executed = false;
        disp.UnhandledException += (sender, args) =>
        {
            handled = true;
            args.Handled = true;
        };

        syncContext.Post(_ => ThrowAnException(), null);
        syncContext.Post(_ => executed = true, null);

        disp.RunJobs(null, TestContext.Current.CancellationToken);

        Assert.True(handled);
        Assert.True(executed);
    }

    [Fact]
    public void CanRemoveDispatcherExceptionHandler()
    {
        var impl = new ManagedDispatcherImpl(null);
        var dispatcher = new Dispatcher(impl);
        var caughtCorrectException = false;

        dispatcher.UnhandledExceptionFilter +=
            HandlerOnUnhandledExceptionFilterRequestCatch;
        dispatcher.UnhandledException +=
            HandlerOnUnhandledExceptionNotHandled;

        dispatcher.UnhandledExceptionFilter -=
            HandlerOnUnhandledExceptionFilterRequestCatch;
        dispatcher.UnhandledException -=
            HandlerOnUnhandledExceptionNotHandled;

        try
        {
            dispatcher.Post(ThrowAnException, DispatcherPriority.Normal);
            dispatcher.RunJobs(null, TestContext.Current.CancellationToken);
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
        var impl = new ManagedDispatcherImpl(null);
        var dispatcher = new Dispatcher(impl);
        
        dispatcher.UnhandledExceptionFilter +=
            HandlerOnUnhandledExceptionFilterRequestCatch;
        
        dispatcher.UnhandledException +=
            HandlerOnUnhandledExceptionHandled;
        var caughtCorrectException = true;
        try
        {
            dispatcher.Post(ThrowAnException, DispatcherPriority.Normal);
            dispatcher.RunJobs(null, TestContext.Current.CancellationToken);
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
        var impl = new ManagedDispatcherImpl(null);
        var dispatcher = new Dispatcher(impl);
        
        dispatcher.UnhandledExceptionFilter +=
            HandlerOnUnhandledExceptionFilterRequestCatch;
        
        dispatcher.UnhandledException +=
            HandlerOnUnhandledExceptionHandled;
        var caughtCorrectException = false;
        try
        {
            // Since both Invoke and InvokeAsync can throw exception, there is no need to pass them to the UnhandledException.
            dispatcher.Invoke(ThrowAnException, DispatcherPriority.Normal, TestContext.Current.CancellationToken);
            dispatcher.RunJobs(null, TestContext.Current.CancellationToken);
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
        var impl = new ManagedDispatcherImpl(null);
        var dispatcher = new Dispatcher(impl);
        
        dispatcher.UnhandledExceptionFilter +=
            HandlerOnUnhandledExceptionFilterRequestCatch;
        
        dispatcher.UnhandledException +=
            HandlerOnUnhandledExceptionHandled;
        var caughtCorrectException = false;
        try
        {
            // Since both Invoke and InvokeAsync can throw exception, there is no need to pass them to the UnhandledException.
            var op = dispatcher.InvokeAsync(ThrowAnException, DispatcherPriority.Normal, TestContext.Current.CancellationToken);
            op.Wait();
            dispatcher.RunJobs(null, TestContext.Current.CancellationToken);
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
        var impl = new ManagedDispatcherImpl(null);
        var dispatcher = new Dispatcher(impl);
        
        dispatcher.UnhandledExceptionFilter +=
            HandlerOnUnhandledExceptionFilterRequestCatch;
        
        dispatcher.UnhandledException +=
            HandlerOnUnhandledExceptionNotHandled;
        var caughtCorrectException = false;
        try
        {
            dispatcher.Post(ThrowAnException, DispatcherPriority.Normal);
            dispatcher.RunJobs(null, TestContext.Current.CancellationToken);
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
        var impl = new ManagedDispatcherImpl(null);
        var dispatcher = new Dispatcher(impl);
        
        dispatcher.UnhandledExceptionFilter +=
            HandlerOnUnhandledExceptionFilterNotRequestCatch;
        dispatcher.UnhandledExceptionFilter +=
            HandlerOnUnhandledExceptionFilterRequestCatch;
        
        dispatcher.UnhandledException +=
            HandlerOnUnhandledExceptionNotHandled;
        dispatcher.UnhandledException +=
            HandlerOnUnhandledExceptionHandled;
        var caughtCorrectException = false;
        try
        {
            dispatcher.Post(ThrowAnException, DispatcherPriority.Normal);
            dispatcher.RunJobs(null, TestContext.Current.CancellationToken);
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
        var impl = new ManagedDispatcherImpl(null);
        var dispatcher = new Dispatcher(impl);
        
        dispatcher.UnhandledExceptionFilter +=
            HandlerOnUnhandledExceptionFilterRequestCatch;
        
        dispatcher.UnhandledException +=
            HandlerOnUnhandledExceptionHandled;
        dispatcher.UnhandledException +=
            HandlerOnUnhandledExceptionNotHandled;
        var caughtCorrectException = true;
        
        try
        {
            dispatcher.Post(ThrowAnException, DispatcherPriority.Normal);
            dispatcher.RunJobs(null, TestContext.Current.CancellationToken);
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
        var impl = new ManagedDispatcherImpl(null);
        var dispatcher = new Dispatcher(impl);
        
        dispatcher.UnhandledExceptionFilter +=
            HandlerOnUnhandledExceptionFilterNotRequestCatchPushFrame;
        
        dispatcher.UnhandledException +=
            HandlerOnUnhandledExceptionHandledPushFrame;
        var caughtCorrectException = false;
        try
        {
            dispatcher.Post(ThrowAnException, DispatcherPriority.Normal);
            dispatcher.RunJobs(null, TestContext.Current.CancellationToken);
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

