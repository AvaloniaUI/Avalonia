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
    private readonly List<Func<TestExecutionContext, Task>> _beforeTest;
    private readonly List<Func<TestExecutionContext, Task>> _afterTest;

    private AvaloniaTestMethodCommand(
        HeadlessUnitTestSession session,
        TestCommand innerCommand,
        List<Func<TestExecutionContext, Task>> beforeTest,
        List<Func<TestExecutionContext, Task>> afterTest)
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
        List<Func<TestExecutionContext, Task>> before,
        List<Func<TestExecutionContext, Task>> after)
    {
        var beforeAndAfterTestCommand = command as BeforeAndAfterTestCommand;
        if (beforeAndAfterTestCommand is not null)
        {
            ref var beforeTest = ref beforeAndAfterTestCommand.BeforeTest();
            if (beforeTest is not null)
            {
                AddBeforeOrAfterAction(beforeTest, before);
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
                AddBeforeOrAfterAction(afterTest, after);
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
        foreach (var beforeTest in _beforeTest)
            await beforeTest(context);
        
        var testMethod = _innerCommand.Test.Method;
        var methodInfo = testMethod!.MethodInfo;

        var result = methodInfo.Invoke(context.TestObject, _innerCommand.Test.Arguments);
        await ToTask(result);

        context.CurrentResult.SetResult(ResultState.Success);

        if (context.CurrentResult.AssertionResults.Count > 0)
            context.CurrentResult.RecordTestCompletion();

        if (context.ExecutionStatus != TestExecutionStatus.AbortRequested)
        {
            foreach (var afterTest in _afterTest)
                await afterTest(context);

            Dispatcher.UIThread.RunJobs();
        }
        
        return context.CurrentResult;
    }

    private static void AddBeforeOrAfterAction(Action<TestExecutionContext> action, List<Func<TestExecutionContext, Task>> targets)
    {
        // We need to extract the SetUp and TearDown methods to run them asynchronously on Avalonia's synchronization context.
        if (action.Target is SetUpTearDownItem setUpTearDownItem)
        {
            var methods = action.Method.Name switch
            {
                nameof(SetUpTearDownItem.RunSetUp) => setUpTearDownItem.SetUpMethods(),
                nameof(SetUpTearDownItem.RunTearDown) => setUpTearDownItem.TearDownMethods(),
                _ => null
            };

            if (methods is not null)
            {
                foreach (var method in methods)
                {
                    targets.Add(context =>
                    {
                        var result = method.Invoke(method.IsStatic ? null : context.TestObject, null);
                        return ToTask(result);
                    });
                }

                return;
            }
        }

        targets.Add(context =>
        {
            action(context);
            return Task.CompletedTask;
        });
    }

    private static Task ToTask(object? result)
        // Only Task, non generic ValueTask are supported in async context. No ValueTask<> nor F# tasks.
        => result switch
        {
            Task task => task,
            ValueTask valueTask => valueTask.AsTask(),
            _ => Task.CompletedTask
        };
}
