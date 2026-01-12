#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Avalonia.UnitTests;

public class ThreadRunHelper
{
    public static Task<T> RunOnDedicatedThread<T>(Func<T> cb)
    {
        // Task.Run(...).GetAwaiter().GetResult() can be inlined, so we have this cursed thing instead
        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        new Thread(() =>
        {
            try
            {
                tcs.SetResult(cb());
            }
            catch (Exception e)
            {
                tcs.SetException(e);
            }
        }).Start();
        return tcs.Task;
    }

    public static Task RunOnDedicatedThread(Action cb) => RunOnDedicatedThread<object?>(() =>
    {
        cb();
        return null;
    });

    public static void RunOnDedicatedThreadAndWait(Action cb) => RunOnDedicatedThread(cb).GetAwaiter().GetResult();
}
