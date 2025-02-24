using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
    private readonly List<Action> _beforeTest;
    private readonly List<Action<TestExecutionContext>> _afterTest;

    // There are multiple problems with NUnit integration at the moment when we wrote this integration.
    // NUnit doesn't have extensibility API for running on custom dispatcher/sync-context.
    // See https://github.com/nunit/nunit/issues/2917 https://github.com/nunit/nunit/issues/2774
    // To workaround that we had to replace inner TestMethodCommand with our own implementation while keeping original hierarchy of commands.
    // Which will respect proper async/await awaiting code that works with our session and can be block-awaited to fit in NUnit.
    // Also, we need to push BeforeTest/AfterTest callbacks to the very same session call.
    // I hope there will be a better solution without reflection, but for now that's it.
    private static FieldInfo s_innerCommand = typeof(DelegatingTestCommand)
        .GetField("innerCommand", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static FieldInfo s_beforeTest = typeof(BeforeAndAfterTestCommand)
        .GetField("BeforeTest", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static FieldInfo s_afterTest = typeof(BeforeAndAfterTestCommand)
        .GetField("AfterTest", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static FieldInfo s_setUpMethods = typeof(SetUpTearDownItem)
        .GetField("_setUpMethods", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static FieldInfo s_tearDownMethods = typeof(SetUpTearDownItem)
        .GetField("_tearDownMethods", BindingFlags.Instance | BindingFlags.NonPublic)!;

    private AvaloniaTestMethodCommand(
        HeadlessUnitTestSession session,
        TestCommand innerCommand,
        List<Action> beforeTest,
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

    private static TestCommand ProcessCommand(HeadlessUnitTestSession session, TestCommand command, List<Action> before, List<Action<TestExecutionContext>> after)
    {
        if (command is BeforeAndAfterTestCommand beforeAndAfterTestCommand)
        {
            if (s_beforeTest.GetValue(beforeAndAfterTestCommand) is Action<TestExecutionContext> beforeTest)
            {
                var setUpTearDownInfo = beforeTest.Target!.GetType()
                    .GetField("setUpTearDown", BindingFlags.Instance | BindingFlags.Public)!;
                if (setUpTearDownInfo.GetValue(beforeTest.Target) is SetUpTearDownItem setUpTearDown
                    && s_setUpMethods.GetValue(setUpTearDown) is List<IMethodInfo> setUpMethods)
                {
                    Action<TestExecutionContext> beforeAction = c =>
                    {
                        before.AddRange(setUpMethods.Select<IMethodInfo, Action>(m => async void () =>
                        {
                            var result = m.Invoke(c.TestObject, []);
                            if (result is Task task)
                            {
                                await task;
                            }
                            else if (result is ValueTask valueTask)
                            {
                                await valueTask;
                            }
                        }));
                    };
                    s_beforeTest.SetValue(beforeAndAfterTestCommand, beforeAction);
                }
                else
                {
                    Action<TestExecutionContext> beforeAction = c => before.Add(() => beforeTest(c));
                    s_beforeTest.SetValue(beforeAndAfterTestCommand, beforeAction);
                }
            }
            
            // Experimentally, after test methods are called after the ExecuteTestMethod has run
            // So rather than add them to a list of actions to execute, we just have the commands execute them
            if (s_afterTest.GetValue(beforeAndAfterTestCommand) is Action<TestExecutionContext> afterTest)
            {
                var setUpTearDownInfo = afterTest.Target!.GetType()
                    .GetField("setUpTearDown", BindingFlags.Instance | BindingFlags.Public)!;
                if (setUpTearDownInfo.GetValue(afterTest.Target) is SetUpTearDownItem setUpTearDown
                    && s_tearDownMethods.GetValue(setUpTearDown) is List<IMethodInfo> tearDownMethods)
                {
                    after.Add(c =>
                    {
                        tearDownMethods.ForEach(async void (m) =>
                        {
                            var result = m.Invoke(c.TestObject, []);
                            if (result is Task task)
                            {
                                await task;
                            }
                            else if (result is ValueTask valueTask)
                            {
                                await valueTask;
                            }
                        });
                    });
                    s_afterTest.SetValue(beforeAndAfterTestCommand, (TestExecutionContext _) => { });
                }
                else
                {
                    after.Add(c => afterTest(c));
                    s_afterTest.SetValue(beforeAndAfterTestCommand, (TestExecutionContext _) => { });
                }
            }
        }
        
        if (command is DelegatingTestCommand delegatingTestCommand
            && s_innerCommand.GetValue(delegatingTestCommand) is TestCommand inner)
        {
            s_innerCommand.SetValue(delegatingTestCommand, ProcessCommand(session, inner, before, after));
        }
        else if (command is TestMethodCommand methodCommand)
        {
            return new AvaloniaTestMethodCommand(session, methodCommand, before, after);
        }

        return command;
    }

    public override TestResult Execute(TestExecutionContext context)
    {
        return _session.DispatchCore(() => ExecuteTestMethod(context), true, default).GetAwaiter().GetResult();
    }

    // Unfortunately, NUnit has issues with custom synchronization contexts, which means we need to add some hacks to make it work.
    private async Task<TestResult> ExecuteTestMethod(TestExecutionContext context)
    {
        _beforeTest.ForEach(a => a());
        
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
