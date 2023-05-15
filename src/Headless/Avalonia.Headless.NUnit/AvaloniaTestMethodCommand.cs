using System;
using System.Collections.Generic;
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
    private readonly List<Action> _afterTest;

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
    
    private AvaloniaTestMethodCommand(
        HeadlessUnitTestSession session,
        TestCommand innerCommand,
        List<Action> beforeTest,
        List<Action> afterTest)
        : base(innerCommand.Test)
    {
        _session = session;
        _innerCommand = innerCommand;
        _beforeTest = beforeTest;
        _afterTest = afterTest;
    }

    public static TestCommand ProcessCommand(HeadlessUnitTestSession session, TestCommand command)
    {
        return ProcessCommand(session, command, new List<Action>(), new List<Action>());
    }
    
    private static TestCommand ProcessCommand(HeadlessUnitTestSession session, TestCommand command, List<Action> before, List<Action> after)
    {
        if (command is BeforeAndAfterTestCommand beforeAndAfterTestCommand)
        {
            if (s_beforeTest.GetValue(beforeAndAfterTestCommand) is Action<TestExecutionContext> beforeTest)
            {
                Action<TestExecutionContext> beforeAction = c => before.Add(() => beforeTest(c));
                s_beforeTest.SetValue(beforeAndAfterTestCommand, beforeAction);
            }
            if (s_afterTest.GetValue(beforeAndAfterTestCommand) is Action<TestExecutionContext> afterTest)
            {
                Action<TestExecutionContext> afterAction = c => after.Add(() => afterTest(c));
                s_afterTest.SetValue(beforeAndAfterTestCommand, afterAction);
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
        return _session.Dispatch(() => ExecuteTestMethod(context), default).GetAwaiter().GetResult();
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
            _afterTest.ForEach(a => a());
        }
        
        return context.CurrentResult;
    }
}
