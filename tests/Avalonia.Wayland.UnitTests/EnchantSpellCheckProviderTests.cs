using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
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

    [Fact]
    public async Task Enchant_Context_Checks_And_Suggests_When_A_Dictionary_Is_Installed()
    {
        using var context = EnchantSpellCheckProvider.Instance.CreateContext(CultureInfo.GetCultureInfo("en-US"));

        Assert.SkipWhen(context is null, "libenchant-2 with an en_US dictionary is not installed.");

        var results = await context.CheckAsync("Ths sample has mispelled wrds".AsMemory(), TestContext.Current.CancellationToken);

        Assert.Equal(new[] { (0, 3), (15, 9), (25, 4) }, results.Select(r => (r.Start, r.Length)));

        var suggestions = await results[0].SuggestAsync(TestContext.Current.CancellationToken);
        Assert.Contains("This", suggestions);

        context.Dispose();
        Assert.Throws<ObjectDisposedException>(() => { _ = results[0].SuggestAsync(TestContext.Current.CancellationToken); });
    }

    [Fact]
    public void Enchant_Returns_No_Context_For_A_Culture_Without_A_Dictionary()
    {
        Assert.Null(EnchantSpellCheckProvider.Instance.CreateContext(CultureInfo.GetCultureInfo("yo-NG")));
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
