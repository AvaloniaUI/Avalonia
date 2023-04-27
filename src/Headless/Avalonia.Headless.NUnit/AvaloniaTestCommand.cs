using System;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;

namespace Avalonia.Headless.NUnit;

internal class AvaloniaTestCommand : DelegatingTestCommand
{
    private readonly HeadlessUnitTestSession _session;

    private static FieldInfo s_beforeTest = typeof(BeforeAndAfterTestCommand)
        .GetField("BeforeTest", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static FieldInfo s_afterTest = typeof(BeforeAndAfterTestCommand)
        .GetField("AfterTest", BindingFlags.Instance | BindingFlags.NonPublic)!;
    
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
        try
        {
            if (innerCommand is BeforeAndAfterTestCommand beforeTestCommand)
            {
                (s_beforeTest.GetValue(beforeTestCommand) as Action<TestExecutionContext>)?.Invoke(context);
            }

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
        finally
        {
            if (innerCommand is BeforeAndAfterTestCommand beforeTestCommand
                && context.ExecutionStatus != TestExecutionStatus.AbortRequested)
            {
                (s_afterTest.GetValue(beforeTestCommand) as Action<TestExecutionContext>)?.Invoke(context);
            }
        }
    }
}
