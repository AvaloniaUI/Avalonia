using System;
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
