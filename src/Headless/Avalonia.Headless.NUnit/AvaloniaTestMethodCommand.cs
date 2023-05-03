using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Threading;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;

namespace Avalonia.Headless.NUnit;

internal class AvaloniaTestMethodCommand : DelegatingTestCommand
{
    private readonly HeadlessUnitTestSession _session;
    private readonly List<Action> _beforeTest;
    private readonly List<Action> _afterTest;

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
        : base(innerCommand)
    {
        _session = session;
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

        if (context.ExecutionStatus != TestExecutionStatus.AbortRequested)
        {
            _afterTest.ForEach(a => a());
        }
        
        return context.CurrentResult;
    }
}
