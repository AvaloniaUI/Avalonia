using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal.Commands;

namespace Avalonia.Headless.NUnit;

/// <summary>
/// Identifies a nunit theory that starts on Avalonia Dispatcher
/// such that awaited expressions resume on the test's "main thread".
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class AvaloniaTheoryAttribute : TheoryAttribute, IWrapSetUpTearDown
{
    public TestCommand Wrap(TestCommand command)
    {
        var session = HeadlessUnitTestSession.GetOrStartForAssembly(command.Test.Method?.MethodInfo.DeclaringType?.Assembly);

        return AvaloniaTestMethodCommand.ProcessCommand(session, command);
    }
}
