using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Threading;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;

namespace Avalonia.Headless.NUnit;

internal class AvaloniaTestMethodCommand : TestCommand
{
    private readonly HeadlessUnitTestSession _session;
    private readonly TestCommand _innerCommand;
    private readonly List<Action<TestExecutionContext>> _beforeTest;
    private readonly List<Action<TestExecutionContext>> _afterTest;

    private AvaloniaTestMethodCommand(
        HeadlessUnitTestSession session,
        TestCommand innerCommand,
        List<Action<TestExecutionContext>> beforeTest,
        List<Action<TestExecutionContext>> afterTest)
        : base(innerCommand.Test)
    {
        _session = session;
        _innerCommand = innerCommand;
        _beforeTest = beforeTest;
        _afterTest = afterTest;
    }

    public static TestCommand ProcessCommand(HeadlessUnitTestSession session, TestCommand command)
    {
        return ProcessCommand(session, command, [], []);
    }

    private static TestCommand ProcessCommand(
        HeadlessUnitTestSession session,
        TestCommand command,
        List<Action<TestExecutionContext>> before,
        List<Action<TestExecutionContext>> after)
    {
        var beforeAndAfterTestCommand = command as BeforeAndAfterTestCommand;
        if (beforeAndAfterTestCommand is not null)
        {
            ref var beforeTest = ref beforeAndAfterTestCommand.BeforeTest();
            if (beforeTest is not null)
            {
                before.Add(beforeTest);
                beforeTest = _ => { };
            }
        }

        var delegatingTestCommand = command as DelegatingTestCommand;
        if (delegatingTestCommand is not null)
        {
            ref var innerCommand = ref delegatingTestCommand.InnerCommand();
            innerCommand = ProcessCommand(session, innerCommand, before, after);
        }

        if (beforeAndAfterTestCommand is not null)
        {
            ref var afterTest = ref beforeAndAfterTestCommand.AfterTest();
            if (afterTest is not null)
            {
                after.Add(afterTest);
                afterTest = _ => { };
            }
        }
        
        if (delegatingTestCommand is null && command is TestMethodCommand methodCommand)
            return new AvaloniaTestMethodCommand(session, methodCommand, before, after);

        return command;
    }

    public override TestResult Execute(TestExecutionContext context)
    {
        return _session.DispatchCore(() => ExecuteTestMethod(context), true, context.CancellationToken).GetAwaiter().GetResult();
    }

    // Unfortunately, NUnit has issues with custom synchronization contexts, which means we need to add some hacks to make it work.
    private async Task<TestResult> ExecuteTestMethod(TestExecutionContext context)
    {
        _beforeTest.ForEach(a => a(context));
        
        var testMethod = _innerCommand.Test.Method;
        var methodInfo = testMethod!.MethodInfo;

        var result = methodInfo.Invoke(context.TestObject, _innerCommand.Test.Arguments);
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

        if (context.ExecutionStatus != TestExecutionStatus.AbortRequested)
        {
            _afterTest.ForEach(a => a(context));
            Dispatcher.UIThread.RunJobs();
        }
        
        return context.CurrentResult;
    }
}
