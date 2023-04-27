using System.Threading.Tasks;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;

namespace Avalonia.Headless.NUnit;

internal class AvaloniaTestCommand : DelegatingTestCommand
{
    private readonly HeadlessUnitTestSession _session;

    public AvaloniaTestCommand(HeadlessUnitTestSession session, TestCommand innerCommand)
        : base(innerCommand)
    {
        _session = session;
    }

    public override TestResult Execute(TestExecutionContext context)
    {
        return _session.Dispatcher.InvokeOnQueue(() => ExecuteTestMethod(context));
    }

    // Unfortunately, NUnit has issues with custom synchronization contexts, which means we need to add some hacks to make it work.
    private async Task<TestResult> ExecuteTestMethod(TestExecutionContext context)
    {
        var testMethod = innerCommand.Test.Method;
        var methodInfo = testMethod!.MethodInfo;

        var result = methodInfo.Invoke(context.TestObject, innerCommand.Test.Arguments);
        // Only Task, non generic ValueTask are supported in async context. No ValueTask<> nor F# tasks.
        if (result is Task task)
        {
            await task;
        }
        else if (result is ValueTask valueTask)
        {
            await valueTask;
        }

        context.CurrentResult.SetResult(ResultState.Success);

        if (context.CurrentResult.AssertionResults.Count > 0)
            context.CurrentResult.RecordTestCompletion();

        return context.CurrentResult;
    }
}
