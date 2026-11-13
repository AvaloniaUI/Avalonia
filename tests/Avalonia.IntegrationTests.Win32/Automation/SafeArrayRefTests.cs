using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Avalonia.Win32.Automation.Marshalling;
using Xunit;

namespace Avalonia.IntegrationTests.Win32.Automation;

public unsafe class SafeArrayRefTests
{
    [Fact]
    public void ConvertToUnmanaged_Sizes_String_Array_To_Input()
    {
        var safeArray = SafeArrayMarshaller<string>.ConvertToUnmanaged(["foo", "bar", "baz"]);

        Assert.Equal(3, GetLength(safeArray));

        SafeArrayMarshaller<string>.Free(safeArray);
    }

    [Fact]
    public void ConvertToUnmanaged_Sizes_Provider_Array_To_Input()
    {
        ITestProvider[] providers = [new TestProvider(1), new TestProvider(2)];

        var safeArray = SafeArrayMarshaller<ITestProvider>.ConvertToUnmanaged(providers);

        Assert.Equal(providers.Length, GetLength(safeArray));

        SafeArrayMarshaller<ITestProvider>.Free(safeArray);
    }

    [Fact]
    public void ConvertToUnmanaged_Fills_Provider_Array_With_Com_Wrappers()
    {
        ITestProvider[] providers = [new TestProvider(1), new TestProvider(2)];

        var safeArray = SafeArrayMarshaller<ITestProvider>.ConvertToUnmanaged(providers);

        // Check the length before reading the entries. A wrongly sized array holds slots that were
        // never written, and dereferencing those below would take down the test host.
        Assert.Equal(providers.Length, GetLength(safeArray));
        Assert.All(GetEntries(safeArray), x => Assert.NotEqual(IntPtr.Zero, x));

        var roundTripped = SafeArrayMarshaller<ITestProvider>.ConvertToManaged(safeArray);
        Assert.NotNull(roundTripped);
        Assert.Equal([1, 2], Array.ConvertAll(roundTripped, x => x.GetValue()));

        SafeArrayMarshaller<ITestProvider>.Free(safeArray);
    }

    private static int GetLength(SafeArrayRef safeArray)
    {
        var ptr = (SafeArrayRef.SAFEARRAY*)Unsafe.As<SafeArrayRef, IntPtr>(ref safeArray);
        return (int)ptr->rgsabound[0].cElements;
    }

    private static IntPtr[] GetEntries(SafeArrayRef safeArray)
    {
        var ptr = (SafeArrayRef.SAFEARRAY*)Unsafe.As<SafeArrayRef, IntPtr>(ref safeArray);
        var result = new IntPtr[(int)ptr->rgsabound[0].cElements];
        new Span<IntPtr>(ptr->pvData, result.Length).CopyTo(result);
        return result;
    }
}

[GeneratedComInterface]
[Guid("6ADEBBF3-6C63-4C1B-9C4F-9EE7C3B8A6D1")]
internal partial interface ITestProvider
{
    int GetValue();
}

[GeneratedComClass]
internal partial class TestProvider : ITestProvider
{
    private readonly int _value;

    public TestProvider(int value) => _value = value;

    public int GetValue() => _value;
}
