using System;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit;
using static Avalonia.IntegrationTests.Win32.UnmanagedMethods;

namespace Avalonia.IntegrationTests.Win32.Infrastructure;

/// <summary>
/// A fact that needs a keyboard layout. On a developer machine, the test is skipped if the user doesn't have the
/// layout installed, so that tests don't change the user's layouts. On CI (<see cref="CiEnvironmentVariable"/> set
/// to 1), the test always runs and loads the layout.
/// </summary>
internal sealed class KeyboardLayoutFactAttribute : FactAttribute
{
    public const string CiEnvironmentVariable = "AVALONIA_CI_RUN";

    public KeyboardLayoutFactAttribute(
        string layoutId,
        [CallerFilePath] string? sourceFilePath = null,
        [CallerLineNumber] int sourceLineNumber = -1)
        : base(sourceFilePath, sourceLineNumber)
    {
        if (Environment.GetEnvironmentVariable(CiEnvironmentVariable) != "1" && !IsLayoutInstalled(layoutId))
            Skip = $"Keyboard layout {layoutId} is not installed. Set {CiEnvironmentVariable}=1 to load it.";
    }

    private static bool IsLayoutInstalled(string layoutId)
    {
        // For a basic layout ID such as 00000407, the HKL has the language ID in both words
        var languageId = ulong.Parse(layoutId, NumberStyles.HexNumber) & 0xFFFF;
        var expected = (languageId << 16) | languageId;

        var count = GetKeyboardLayoutList(0, null);
        var layouts = new IntPtr[Math.Max(count, 0)];
        count = GetKeyboardLayoutList(layouts.Length, layouts);
        return layouts.Take(count).Any(hkl => ((ulong)hkl.ToInt64() & 0xFFFFFFFF) == expected);
    }
}
