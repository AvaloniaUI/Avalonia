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
        return _session.Dispatcher.InvokeAsync<Task<TestResult>>(async () =>
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
        }).GetTask().Unwrap().Result;
    }
}
