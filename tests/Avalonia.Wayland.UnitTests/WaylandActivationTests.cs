using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Avalonia.Wayland.Server;
using Xunit;

namespace Avalonia.Wayland.UnitTests;

public class WaylandActivationTests : ScopedTestBase
{
    private sealed class Harness
    {
        public readonly Queue<Action> Worker = new(), UI = new(), Batch = new();
        public readonly List<(WaylandActivation.Target Target, string Token)> Requests = new();
        public readonly WaylandActivation Activation;
        public object Connection = new();
        public bool Available = true;

        public Harness()
        {
            Activation = new(Worker.Enqueue, UI.Enqueue, () => Available);
            Activation.OnConnected(Connection, notifyUI: false);
            Activation.Initialize(Connection); // Initial startup handshake; no window or dispatcher needed.
        }

        public WaylandActivation.Target Target(Func<object?>? connection = null)
        {
            WaylandActivation.Target target = null!;
            target = new(connection ?? (() => Connection), token => Requests.Add((target, token)));
            return target;
        }

        public void Commit(WaylandActivation.Target target)
        {
            var connection = Connection;
            Batch.Enqueue(() => Activation.BufferCommitted(target, connection));
        }

        public void Reconnect()
        {
            Activation.OnDisconnected();
            Connection = new();
            Activation.OnConnected(Connection, notifyUI: true);
        }

        public static void Run(Queue<Action> queue)
        {
            while (queue.TryDequeue(out var action))
                action();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Latest_delivery_is_consumed_once_and_waits_for_commit(bool alreadyMapped)
    {
        var h = new Harness();
        var target = h.Target();
        if (alreadyMapped)
        {
            h.Commit(target);
            Harness.Run(h.Batch);
        }
        h.Activation.Deliver("A");
        h.Activation.Deliver("B");
        h.Activation.Activate(target);
        h.Activation.Activate(target);
        Assert.Empty(h.Requests);
        Harness.Run(h.Worker);
        Assert.Equal(alreadyMapped ? 1 : 0, h.Requests.Count);
        h.Commit(target);
        Harness.Run(h.Batch);
        Assert.Equal((target, "B"), Assert.Single(h.Requests));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void New_delivery_or_empty_delivery_does_not_change_an_already_selected_target(bool empty)
    {
        var h = new Harness();
        var first = h.Target();
        var second = h.Target();
        h.Activation.Deliver("A");
        h.Activation.Activate(first);
        h.Activation.Deliver(empty ? "" : "B");
        h.Activation.Activate(second);
        Harness.Run(h.Worker);
        h.Commit(first);
        h.Commit(second);
        Harness.Run(h.Batch);
        Assert.Equal((first, "A"), h.Requests[0]);
        Assert.Equal(empty ? 1 : 2, h.Requests.Count);
        if (!empty)
            Assert.Equal((second, "B"), h.Requests[1]);
    }

    [Fact]
    public void Repeated_activation_replaces_pending_token_only_when_a_new_token_exists()
    {
        var h = new Harness();
        var target = h.Target();
        h.Activation.Deliver("A");
        h.Activation.Activate(target);
        h.Activation.Deliver("B");
        h.Activation.Activate(target);
        h.Activation.Activate(target);
        Harness.Run(h.Worker);
        h.Commit(target);
        Harness.Run(h.Batch);
        Assert.Equal((target, "B"), Assert.Single(h.Requests));
    }

    [Fact]
    public void Invalid_and_cancelled_targets_leave_token_for_the_next_valid_target()
    {
        var h = new Harness();
        var invalid = h.Target(() => null);
        var oldConnection = h.Target(() => new object());
        var cancelled = h.Target();
        var valid = h.Target();
        h.Activation.Cancel(cancelled);
        h.Activation.Deliver("A");
        h.Activation.Activate(invalid);
        h.Activation.Activate(oldConnection);
        h.Activation.Activate(cancelled);
        h.Activation.Activate(valid);
        Harness.Run(h.Worker);
        h.Commit(valid);
        Harness.Run(h.Batch);
        Assert.Equal((valid, "A"), Assert.Single(h.Requests));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Cancellation_is_ordered_with_activation_but_cannot_retract_a_submitted_request(bool submitFirst)
    {
        var h = new Harness();
        var target = h.Target();
        h.Activation.Deliver("A");
        h.Activation.Activate(target);
        h.Commit(target);
        if (submitFirst)
        {
            Harness.Run(h.Worker);
            Harness.Run(h.Batch);
        }
        h.Activation.Cancel(target);
        h.Activation.Activate(target); // Cannot resurrect the cancelled lifecycle.
        h.Batch.Enqueue(() => h.Activation.SurfaceDisconnected(target));
        Harness.Run(h.Worker);
        Harness.Run(h.Batch);
        Assert.Equal(submitFirst ? 1 : 0, h.Requests.Count);

        var reopened = h.Target();
        h.Activation.Deliver("B");
        h.Activation.Activate(reopened);
        Harness.Run(h.Worker);
        h.Commit(reopened);
        Harness.Run(h.Batch);
        Assert.Equal((reopened, "B"), h.Requests[^1]);
    }

    [Fact]
    public void Cancelling_one_target_preserves_other_targets_and_unconsumed_deliveries()
    {
        var h = new Harness();
        var first = h.Target();
        var second = h.Target();
        h.Activation.Deliver("A");
        h.Activation.Activate(first);
        h.Activation.Deliver("B");
        h.Activation.Cancel(first);
        h.Activation.Activate(second);
        Harness.Run(h.Worker);
        h.Commit(first);
        h.Commit(second);
        Harness.Run(h.Batch);
        Assert.Equal((second, "B"), Assert.Single(h.Requests));
    }

    [Fact]
    public void Reconnect_rejects_queued_deliveries_and_messages_sent_before_UI_receives_the_new_connection()
    {
        var h = new Harness();
        h.Activation.Deliver("old queued token");
        h.Reconnect();
        h.Activation.Deliver("old UI identity");
        var target = h.Target();
        Harness.Run(h.UI);
        h.Activation.Activate(target);
        Harness.Run(h.Worker); // Old work is retained, not cleared from the queue.
        h.Commit(target);
        Harness.Run(h.Batch);
        Assert.Empty(h.Requests);
        h.Activation.Deliver("new");
        h.Activation.Activate(target);
        Harness.Run(h.Worker);
        Assert.Equal((target, "new"), Assert.Single(h.Requests));
    }

    [Fact]
    public void Reconnect_rejects_old_activation_cancel_and_deferred_commit_without_poisoning_new_requests()
    {
        var h = new Harness();
        var target = h.Target();
        h.Activation.Deliver("old");
        h.Activation.Activate(target);
        Harness.Run(h.Worker);
        h.Commit(target);
        h.Activation.Activate(target);
        h.Activation.Cancel(target);
        h.Activation.SurfaceDisconnected(target);
        h.Reconnect();
        Harness.Run(h.UI);
        h.Activation.Deliver("new");
        h.Activation.Activate(target);
        Harness.Run(h.Worker);
        Harness.Run(h.Batch); // The old connection's commit must not map the new target.
        Assert.Empty(h.Requests);
        h.Commit(target);
        Harness.Run(h.Batch);
        Assert.Equal((target, "new"), Assert.Single(h.Requests));
    }

    [Fact]
    public void Late_invalidation_does_not_clear_an_installed_new_connection()
    {
        var h = new Harness();
        h.Reconnect();
        var oldInvalidation = h.UI.Dequeue();
        Harness.Run(h.UI);
        oldInvalidation(); // Notification identity check, independent of notification transport order.
        var target = h.Target();
        h.Activation.Deliver("new");
        h.Activation.Activate(target);
        Harness.Run(h.Worker);
        h.Commit(target);
        Harness.Run(h.Batch);
        Assert.Equal((target, "new"), Assert.Single(h.Requests));
    }

    [Fact]
    public void Protocol_removal_clears_managed_state_and_missing_protocol_drops_deliveries()
    {
        var h = new Harness();
        var target = h.Target();
        h.Activation.Deliver("selected");
        h.Activation.Activate(target);
        h.Activation.Deliver("unselected");
        Harness.Run(h.Worker);
        h.Available = false;
        h.Activation.Clear();
        h.Activation.Deliver("no protocol");
        Harness.Run(h.Worker);
        h.Available = true;
        h.Activation.Activate(target);
        Harness.Run(h.Worker);
        h.Commit(target);
        Harness.Run(h.Batch);
        Assert.Empty(h.Requests);
        h.Activation.Deliver("restored");
        h.Activation.Activate(target);
        Harness.Run(h.Worker);
        Assert.Equal((target, "restored"), Assert.Single(h.Requests));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Another_activation_can_consume_the_token_before_posted_work(bool newDelivery)
    {
        var h = new Harness();
        var first = h.Target();
        var later = h.Target();
        h.Activation.Deliver("A");
        Dispatcher.UIThread.Post(() => h.Activation.Activate(later));
        h.Activation.Activate(first);
        if (newDelivery)
            h.Activation.Deliver("B");
        Dispatcher.UIThread.RunJobs();
        Harness.Run(h.Worker);
        h.Commit(first);
        h.Commit(later);
        Harness.Run(h.Batch);
        Assert.Equal((first, "A"), h.Requests[0]);
        Assert.Equal(newDelivery ? 2 : 1, h.Requests.Count);
        if (newDelivery)
            Assert.Equal((later, "B"), h.Requests[1]);
    }

    [Fact]
    public void Surface_replacement_discards_the_old_target_without_consuming_a_new_delivery()
    {
        var h = new Harness();
        object? oldSurfaceConnection = h.Connection;
        var old = h.Target(() => oldSurfaceConnection);
        var replacement = h.Target();
        h.Activation.Deliver("A");
        h.Activation.Activate(old);
        Harness.Run(h.Worker);
        h.Activation.SurfaceDisconnected(old);
        oldSurfaceConnection = null;
        h.Activation.Deliver("B");
        h.Activation.Activate(old);
        h.Activation.Activate(replacement);
        Harness.Run(h.Worker);
        h.Commit(old);
        h.Commit(replacement);
        Harness.Run(h.Batch);
        Assert.Equal((replacement, "B"), Assert.Single(h.Requests));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Dispatcher_post_and_suspending_await_can_activate_after_the_callback_returns(bool awaitFirst)
    {
        var h = new Harness();
        var target = h.Target();
        var gate = new TaskCompletionSource();
        using var context = AvaloniaSynchronizationContext.Ensure(DispatcherPriority.Normal);
        async Task Later()
        {
            await gate.Task;
            Dispatcher.UIThread.VerifyAccess();
            h.Activation.Activate(target);
        }
        h.Activation.Deliver("A");
        var pending = awaitFirst ? Later() : Task.CompletedTask;
        if (!awaitFirst)
            Dispatcher.UIThread.Post(() => h.Activation.Activate(target));
        Harness.Run(h.Worker);
        Assert.Empty(h.Requests);
        gate.SetResult();
        Dispatcher.UIThread.RunJobs();
        Assert.True(pending.IsCompletedSuccessfully);
        Harness.Run(h.Worker);
        h.Commit(target);
        Harness.Run(h.Batch);
        Assert.Equal((target, "A"), Assert.Single(h.Requests));
    }
}
