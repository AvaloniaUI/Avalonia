using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Avalonia.FreeDesktop;
using Avalonia.Input.TextInput;
using Xunit;

namespace Avalonia.Wayland.UnitTests;

public class EnchantSpellCheckProviderTests
{
    [Fact]
    public void Wayland_Resolves_The_Shared_Provider_Without_A_Native_Connection()
    {
        // Feature resolution must not depend on a surface or connect to a compositor.
        var window = (WindowImpl)RuntimeHelpers.GetUninitializedObject(typeof(WindowImpl));

        Assert.Same(EnchantSpellCheckProvider.Instance, window.TryGetFeature(typeof(ISpellCheckProvider)));
    }

    [Theory]
    [InlineData("jnqz@example.com plain", "plain")]
    [InlineData("https://jnqz.example/pathx wrng", "wrng")]
    [InlineData("identifier_name version2 wrng", "wrng")]
    [InlineData("lead,jnqz@example.com,tail", "lead|tail")]
    [InlineData("hello: wrng", "hello|wrng")]
    [InlineData("plain. (wrng!)", "plain|wrng")]
    public void Only_Natural_Language_Segments_Are_Checkable(string text, string expected)
    {
        var words = new EnchantSpellCheckProvider.CheckableWordEnumerator(text.AsSpan());
        var actual = new List<string>();

        while (words.MoveNext(out var offset, out var length))
        {
            actual.Add(text.Substring(offset, length));
        }

        Assert.Equal(expected, string.Join("|", actual));
    }
}
