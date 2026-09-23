using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Input.TextInput;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class SpellCheckTests : ScopedTestBase
    {
        [Fact]
        public void Spell_Check_Suggestions_Are_Not_Inherited()
        {
            var parent = new Border();
            var child = new TextBox();
            parent.Child = child;

            SpellCheck.SetSuggestions(parent, new[] { "This" });

            Assert.True(SpellCheck.GetHasSuggestions(parent));
            Assert.Empty(SpellCheck.GetSuggestions(child));
            Assert.False(SpellCheck.GetHasSuggestions(child));
        }

        [Fact]
        public void Has_Suggestions_Follows_Suggestions_And_Null_Clears_Them()
        {
            var target = new TextBox();

            SpellCheck.SetSuggestions(target, new[] { "This" });
            Assert.True(SpellCheck.GetHasSuggestions(target));

            SpellCheck.SetSuggestions(target, null);
            Assert.Empty(SpellCheck.GetSuggestions(target));
            Assert.False(SpellCheck.GetHasSuggestions(target));
            Assert.False(target.IsSet(SpellCheck.SuggestionsProperty));
        }

        [Fact]
        public async Task Offset_Result_Forwards_Description_And_Suggestions()
        {
            var source = new DescribedSpellCheckResult();

            var moved = OffsetSpellCheckResult.Create(source, 10, 3);
            var movedAgain = OffsetSpellCheckResult.Create(moved, 20, 3);

            Assert.Same(source, OffsetSpellCheckResult.Create(source, source.Start, source.Length));
            Assert.Equal(10, moved.Start);
            Assert.Equal(20, movedAgain.Start);
            Assert.Equal("Grammar note", movedAgain.Description);
            Assert.Equal(new[] { "This" }, await movedAgain.SuggestAsync(TestContext.Current.CancellationToken));
        }

        private sealed class DescribedSpellCheckResult : ISpellCheckResult
        {
            public int Start => 0;

            public int Length => 3;

            public string? Description => "Grammar note";

            public ValueTask<IReadOnlyList<string>> SuggestAsync(CancellationToken cancellationToken = default) =>
                new(new[] { "This" });
        }
    }
}
