using System;
using System.Runtime.CompilerServices;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;

namespace Avalonia.Headless.NUnit;

/// <summary>
/// 2023-05-10, original comment from Max about NUnit 3:
/// There are multiple problems with NUnit integration at the moment when we wrote this integration.
/// NUnit doesn't have extensibility API for running on custom dispatcher/sync-context.
/// See https://github.com/nunit/nunit/issues/2917 https://github.com/nunit/nunit/issues/2774
/// To workaround that we had to replace inner TestMethodCommand with our own implementation while keeping original hierarchy of commands.
/// Which will respect proper async/await awaiting code that works with our session and can be block-awaited to fit in NUnit.
/// Also, we need to push BeforeTest/AfterTest callbacks to the very same session call.
/// I hope there will be a better solution without reflection, but for now that's it.
///
/// 2026-02-04: the situation hasn't changed at all with NUnit 4.
/// </summary>
internal static class NUnitReflection
{
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = ReflectionDelegatingTestCommand.InnerCommandFieldName)]
    private static extern ref TestCommand DelegatingTestCommand_InnerCommand(DelegatingTestCommand instance);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = ReflectionBeforeAndAfterTestCommand.BeforeTestFieldName)]
    private static extern ref Action<TestExecutionContext>? BeforeAndAfterTestCommand_BeforeTest(BeforeAndAfterTestCommand instance);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = ReflectionBeforeAndAfterTestCommand.AfterTestFieldName)]
    private static extern ref Action<TestExecutionContext>? BeforeAndAfterTestCommand_AfterTest(BeforeAndAfterTestCommand instance);

    extension(DelegatingTestCommand instance)
    {
        public ref TestCommand InnerCommand()
            => ref DelegatingTestCommand_InnerCommand(instance);
    }

    extension(BeforeAndAfterTestCommand instance)
    {
        public ref Action<TestExecutionContext>? BeforeTest()
            => ref BeforeAndAfterTestCommand_BeforeTest(instance);

        public ref Action<TestExecutionContext>? AfterTest()
            => ref BeforeAndAfterTestCommand_AfterTest(instance);
    }

    private sealed class ReflectionDelegatingTestCommand : DelegatingTestCommand
    {
        public ReflectionDelegatingTestCommand(TestCommand innerCommand)
            : base(innerCommand)
        {
        }

        public const string InnerCommandFieldName = nameof(innerCommand);

        public override TestResult Execute(TestExecutionContext context)
            => throw new NotSupportedException("Reflection-only type, this method should never be called");
    }

    private sealed class ReflectionBeforeAndAfterTestCommand : BeforeAndAfterTestCommand
    {
        public ReflectionBeforeAndAfterTestCommand(TestCommand innerCommand)
            : base(innerCommand)
        {
        }

        public const string BeforeTestFieldName = nameof(BeforeTest);
        public const string AfterTestFieldName = nameof(AfterTest);
    }
}
