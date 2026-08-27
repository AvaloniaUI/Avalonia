using System;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Layout;

public class LayoutManagerTests_BringIntoView : ScopedTestBase
{
    [Fact]
    public void Request_Is_Executed_At_End_Of_Layout_Pass()
    {
        var control = new LayoutTestControl();
        var root = new LayoutTestRoot { Child = control };
        root.LayoutManager.ExecuteInitialLayoutPass();

        var request = new TestRequest(control);
        GetLayoutManager(root).EnqueueBringIntoView(request);

        Assert.Equal(0, request.ExecuteAttempts);

        root.LayoutManager.ExecuteLayoutPass();

        Assert.Equal(1, request.ExecuteAttempts);
        Assert.Equal(1, request.Executions);

        root.LayoutManager.ExecuteLayoutPass();

        // Should not have been executed twice.
        Assert.Equal(1, request.ExecuteAttempts);
        Assert.Equal(1, request.Executions);
    }

    [Fact]
    public void Request_Is_Executed_Before_LayoutUpdated_Is_Raised()
    {
        var control = new LayoutTestControl();
        var root = new LayoutTestRoot { Child = control };
        root.LayoutManager.ExecuteInitialLayoutPass();

        var layoutUpdatedRaised = false;
        var executedBeforeLayoutUpdated = false;
        root.LayoutManager.LayoutUpdated += (_, _) => layoutUpdatedRaised = true;

        var request = new TestRequest(control)
        {
            OnExecute = () => executedBeforeLayoutUpdated = !layoutUpdatedRaised,
        };

        GetLayoutManager(root).EnqueueBringIntoView(request);
        root.LayoutManager.ExecuteLayoutPass();

        Assert.Equal(1, request.Executions);
        Assert.True(executedBeforeLayoutUpdated);
    }

    [Fact]
    public void Request_Is_Retained_Until_It_Can_Execute()
    {
        var control = new LayoutTestControl();
        var root = new LayoutTestRoot { Child = control };
        root.LayoutManager.ExecuteInitialLayoutPass();

        var canExecute = false;
        var request = new TestRequest(control) { CanExecute = () => canExecute };
        GetLayoutManager(root).EnqueueBringIntoView(request);

        root.LayoutManager.ExecuteLayoutPass();

        Assert.Equal(1, request.ExecuteAttempts);
        Assert.Equal(0, request.Executions);

        root.LayoutManager.ExecuteLayoutPass();

        Assert.Equal(2, request.ExecuteAttempts);
        Assert.Equal(0, request.Executions);

        canExecute = true;
        root.LayoutManager.ExecuteLayoutPass();

        Assert.Equal(3, request.ExecuteAttempts);
        Assert.Equal(1, request.Executions);
    }

    [Fact]
    public void Requests_Are_Coalesced_By_Target()
    {
        var control = new LayoutTestControl();
        var root = new LayoutTestRoot { Child = control };
        root.LayoutManager.ExecuteInitialLayoutPass();

        var first = new TestRequest(control);
        var second = new TestRequest(control);
        var layoutManager = GetLayoutManager(root);

        layoutManager.EnqueueBringIntoView(first);
        layoutManager.EnqueueBringIntoView(second);

        root.LayoutManager.ExecuteLayoutPass();

        Assert.Equal(0, first.ExecuteAttempts);
        Assert.Equal(1, second.Executions);
    }

    [Fact]
    public void Request_Is_Dropped_When_Target_Is_Detached()
    {
        var control = new LayoutTestControl();
        var root = new LayoutTestRoot { Child = control };
        root.LayoutManager.ExecuteInitialLayoutPass();

        var request = new TestRequest(control);
        GetLayoutManager(root).EnqueueBringIntoView(request);

        root.Child = null;
        root.LayoutManager.ExecuteLayoutPass();

        Assert.Equal(0, request.ExecuteAttempts);

        root.Child = control;
        root.LayoutManager.ExecuteLayoutPass();

        Assert.Equal(0, request.ExecuteAttempts);
    }

    [Fact]
    public void Layout_Invalidated_By_Request_Converges_Within_Same_Pass()
    {
        var control = new LayoutTestControl();
        var root = new LayoutTestRoot { Child = control };
        root.LayoutManager.ExecuteInitialLayoutPass();

        control.Measured = control.Arranged = false;

        var request = new TestRequest(control) { OnExecute = control.InvalidateMeasure };
        GetLayoutManager(root).EnqueueBringIntoView(request);

        root.LayoutManager.ExecuteLayoutPass();

        // The layout invalidated by the request (e.g. a scroll offset change) has been
        // re-run before ExecuteLayoutPass returned, so the frame is rendered fully scrolled.
        Assert.Equal(1, request.Executions);
        Assert.True(control.Measured);
        Assert.True(control.Arranged);
    }

    [Fact]
    public void LayoutUpdated_Is_Not_Raised_Until_All_Requests_Have_Been_Processed()
    {
        var first = new LayoutTestControl();
        var second = new LayoutTestControl();
        var root = new LayoutTestRoot { Child = new StackPanel { Children = { first, second } } };
        root.LayoutManager.ExecuteInitialLayoutPass();

        var layoutManager = GetLayoutManager(root);
        var layoutUpdatedRaised = false;
        var secondExecutedBeforeLayoutUpdated = false;
        root.LayoutManager.LayoutUpdated += (_, _) => layoutUpdatedRaised = true;

        // The second request can only execute once the layout invalidated by the first one has
        // been re-run, so it is executed by a following pass of the processing loop.
        first.Measured = false;

        var secondRequest = new TestRequest(second)
        {
            CanExecute = () => first.Measured,
            OnExecute = () => secondExecutedBeforeLayoutUpdated = !layoutUpdatedRaised,
        };

        var firstRequest = new TestRequest(first) { OnExecute = first.InvalidateMeasure };

        layoutManager.EnqueueBringIntoView(firstRequest);
        layoutManager.EnqueueBringIntoView(secondRequest);

        root.LayoutManager.ExecuteLayoutPass();

        Assert.Equal(1, secondRequest.Executions);
        Assert.True(secondExecutedBeforeLayoutUpdated);
    }

    [Fact]
    public void LayoutUpdated_Is_Raised_Once_When_Request_Invalidates_Layout()
    {
        var control = new LayoutTestControl();
        var root = new LayoutTestRoot { Child = control };
        root.LayoutManager.ExecuteInitialLayoutPass();

        var layoutUpdated = 0;
        root.LayoutManager.LayoutUpdated += (_, _) => ++layoutUpdated;

        var request = new TestRequest(control) { OnExecute = control.InvalidateMeasure };
        GetLayoutManager(root).EnqueueBringIntoView(request);

        root.LayoutManager.ExecuteLayoutPass();

        Assert.Equal(1, request.Executions);
        Assert.Equal(1, layoutUpdated);
    }

    [Fact]
    public void Request_Enqueued_While_Processing_Is_Attempted_By_The_Same_Pass()
    {
        var first = new LayoutTestControl();
        var second = new LayoutTestControl();
        var root = new LayoutTestRoot { Child = new StackPanel { Children = { first, second } } };
        root.LayoutManager.ExecuteInitialLayoutPass();

        var layoutManager = GetLayoutManager(root);
        var canExecuteSecond = false;
        var secondRequest = new TestRequest(second) { CanExecute = () => canExecuteSecond };

        // Executing a request can enqueue another one (as ControlExtensions.BringIntoViewCore does):
        // the new request must be attempted by the same pass, and retained if it can't execute yet.
        var firstRequest = new TestRequest(first)
        {
            OnExecute = () =>
            {
                layoutManager.EnqueueBringIntoView(secondRequest);
                first.InvalidateMeasure();
            }
        };

        layoutManager.EnqueueBringIntoView(firstRequest);
        root.LayoutManager.ExecuteLayoutPass();

        Assert.Equal(1, firstRequest.Executions);

        // Attempted first during the same pass as the first request, then retried once after a new layout pass.
        Assert.Equal(2, secondRequest.ExecuteAttempts);
        Assert.Equal(0, secondRequest.Executions);

        // The second request couldn't execute despite having been through an extra layout pass.
        // We can't retry forever (nothing has changed). The request will be retried again on the next "natural" pass.
        canExecuteSecond = true;
        root.LayoutManager.ExecuteLayoutPass();

        Assert.Equal(3, secondRequest.ExecuteAttempts);
        Assert.Equal(1, secondRequest.Executions);
    }

    private static LayoutManager GetLayoutManager(TestRoot root)
        => Assert.IsType<LayoutManager>(root.LayoutManager, exactMatch: false);

    private sealed class TestRequest(Layoutable target) : BringIntoViewRequest(target)
    {
        public int ExecuteAttempts { get; private set; }
        public int Executions { get; private set; }
        public Func<bool>? CanExecute { get; init; }
        public Action? OnExecute { get; init; }

        public override bool TryExecute()
        {
            ++ExecuteAttempts;

            var canExecute = CanExecute?.Invoke() ?? true;
            if (!canExecute)
                return false;

            ++Executions;
            OnExecute?.Invoke();
            return true;
        }
    }
}
