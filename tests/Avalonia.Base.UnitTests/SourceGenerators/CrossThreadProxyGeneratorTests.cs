using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.SourceGenerator;
using Xunit;

namespace Avalonia.Base.UnitTests.SourceGenerators;

public class CrossThreadProxyGeneratorTests
{
    public enum TestPriority { Low, Normal, High }

    [GenerateCrossThreadProxy(typeof(TestPriority), "Avalonia.Base.UnitTests.SourceGenerators.CrossThreadProxyGeneratorTests.TestPriority.Normal")]
    public interface IBaseProxied
    {
        void BaseFireAndForget(int x);
    }

    [GenerateCrossThreadProxy(typeof(TestPriority), "Avalonia.Base.UnitTests.SourceGenerators.CrossThreadProxyGeneratorTests.TestPriority.Normal")]
    public interface IDerivedProxied : IBaseProxied
    {
        void Increment();
        int GetValue();

        [GenerateCrossThreadProxyReturnTask]
        void AsyncFireAndForget(string s);
    }

    private sealed class Target : IDerivedProxied
    {
        public List<int> BaseCalls { get; } = new();
        public int Counter;
        public List<string> AsyncCalls { get; } = new();
        public Func<int>? GetValueImpl;

        public void BaseFireAndForget(int x) => BaseCalls.Add(x);
        public void Increment() => Counter++;
        public int GetValue() => GetValueImpl?.Invoke() ?? Counter;
        public void AsyncFireAndForget(string s) => AsyncCalls.Add(s);
    }

    private sealed class QueueMarshaller
    {
        public readonly Queue<(Action action, TestPriority priority)> Queue = new();
        public void Post(Action a, TestPriority p) => Queue.Enqueue((a, p));
        public void DrainAll()
        {
            while (Queue.Count > 0) Queue.Dequeue().action();
        }
    }

    [Fact]
    public void Fire_and_forget_void_routes_through_marshaller_with_default_priority()
    {
        var t = new Target();
        var m = new QueueMarshaller();
        var proxy = new DerivedProxiedProxy(t, m.Post);

        proxy.Increment();

        Assert.Single(m.Queue);
        Assert.Equal(TestPriority.Normal, m.Queue.Peek().priority);
        Assert.Equal(0, t.Counter);
        m.DrainAll();
        Assert.Equal(1, t.Counter);
    }

    [Fact]
    public void Explicit_priority_overload_is_used()
    {
        var t = new Target();
        var m = new QueueMarshaller();
        var proxy = new DerivedProxiedProxy(t, m.Post);

        proxy.Increment(TestPriority.High);

        Assert.Equal(TestPriority.High, m.Queue.Peek().priority);
    }

    [Fact]
    public async Task NonVoid_returns_Task_completed_after_marshaller_runs()
    {
        var t = new Target { Counter = 42 };
        var m = new QueueMarshaller();
        var proxy = new DerivedProxiedProxy(t, m.Post);

        var task = proxy.GetValue();
        Assert.False(task.IsCompleted);
        m.DrainAll();
        Assert.True(task.IsCompleted);
        Assert.Equal(42, await task);
    }

    [Fact]
    public void Exception_in_target_propagates_to_Task()
    {
        var t = new Target { GetValueImpl = () => throw new InvalidOperationException("boom") };
        var m = new QueueMarshaller();
        var proxy = new DerivedProxiedProxy(t, m.Post);

        var task = proxy.GetValue();
        m.DrainAll();
        Assert.True(task.IsFaulted);
        Assert.IsType<InvalidOperationException>(task.Exception!.InnerException);
    }

    [Fact]
    public void Void_method_with_ReturnTask_attribute_returns_Task()
    {
        var t = new Target();
        var m = new QueueMarshaller();
        var proxy = new DerivedProxiedProxy(t, m.Post);

        Task task = proxy.AsyncFireAndForget("hi");
        Assert.False(task.IsCompleted);
        m.DrainAll();
        Assert.True(task.IsCompleted);
        Assert.Equal(new[] { "hi" }, t.AsyncCalls);
    }

    [Fact]
    public void Inherited_method_routes_through_base_proxy()
    {
        var t = new Target();
        var m = new QueueMarshaller();
        var proxy = new DerivedProxiedProxy(t, m.Post);

        proxy.BaseFireAndForget(7);
        Assert.Single(m.Queue);
        m.DrainAll();
        Assert.Equal(new[] { 7 }, t.BaseCalls);
    }

    [Fact]
    public void Derived_proxy_is_assignable_to_base_proxy()
    {
        var t = new Target();
        var m = new QueueMarshaller();
        BaseProxiedProxy proxy = new DerivedProxiedProxy(t, m.Post);

        proxy.BaseFireAndForget(3);
        m.DrainAll();
        Assert.Equal(new[] { 3 }, t.BaseCalls);
    }

    [Fact]
    public void Marshaller_is_not_invoked_synchronously_during_proxy_call()
    {
        var t = new Target();
        var m = new QueueMarshaller();
        var proxy = new DerivedProxiedProxy(t, m.Post);

        proxy.Increment();
        proxy.Increment();
        proxy.Increment();

        Assert.Equal(0, t.Counter);
        Assert.Equal(3, m.Queue.Count);
    }
}
