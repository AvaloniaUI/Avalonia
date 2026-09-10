using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Input.TextInput;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class SpellCheckResultCacheTests : ScopedTestBase
    {
        private static SpellCheckResultCache CreateChecked(string text, params (int Start, int Length)[] misspellings)
        {
            var cache = new SpellCheckResultCache();
            var ranges = new List<SpellCheckRange> { new(0, text.Length) };
            var results = misspellings.Select(m => new SpellCheckResult(m.Start, m.Length)).ToList();

            cache.Set(text, ranges, results, merge: false);
            return cache;
        }

        private static string Decorated(SpellCheckResultCache cache, string text) =>
            string.Join("|", cache.Results.Select(r => text.Substring(r.Start, r.Length)));

        private static string Unchecked(SpellCheckResultCache cache, string text)
        {
            var unchecked_ = cache.GetUncheckedRanges(text, new List<SpellCheckRange> { new(0, text.Length) });
            return string.Join("|", unchecked_.Select(r => text.Substring(r.Start, r.End - r.Start)));
        }

        [Fact]
        public void Insert_Before_A_Misspelling_Shifts_It_And_Rechecks_Only_The_Edited_Word()
        {
            var cache = CreateChecked("a wrod here", (2, 4));

            Assert.True(cache.TryApplyEdit("a wrod here", "abc wrod here", trackEditedWord: true));

            Assert.Equal("wrod", Decorated(cache, "abc wrod here"));
            Assert.Equal("abc", Unchecked(cache, "abc wrod here"));
            Assert.Equal((0, 3), (cache.LastEditedWord!.Value.Start, cache.LastEditedWord.Value.End));
        }

        [Fact]
        public void Typing_Inside_A_Misspelling_Drops_It_Until_Rechecked()
        {
            var cache = CreateChecked("Ths sample wrod here", (0, 3), (11, 4));

            Assert.True(cache.TryApplyEdit("Ths sample wrod here", "Ths sample wrods here", trackEditedWord: true));

            Assert.Equal("Ths", Decorated(cache, "Ths sample wrods here"));
            Assert.Equal("wrods", Unchecked(cache, "Ths sample wrods here"));
        }

        [Fact]
        public void Deleting_Across_Two_Words_Invalidates_The_Joined_Word_And_Shifts_The_Rest()
        {
            var cache = CreateChecked("foo bar baz", (0, 3), (4, 3), (8, 3));

            // "foo bar baz" -> "foar baz" (deleted "o b").
            Assert.True(cache.TryApplyEdit("foo bar baz", "foar baz", trackEditedWord: true));

            Assert.Equal("baz", Decorated(cache, "foar baz"));
            Assert.Equal("foar", Unchecked(cache, "foar baz"));
        }

        [Fact]
        public void Joining_Two_Words_By_Deleting_The_Space_Rechecks_The_Joined_Word()
        {
            var cache = CreateChecked("foo bar", (0, 3), (4, 3));

            Assert.True(cache.TryApplyEdit("foo bar", "foobar", trackEditedWord: true));

            Assert.Empty(cache.Results);
            Assert.Equal("foobar", Unchecked(cache, "foobar"));
        }

        [Fact]
        public void Edit_At_A_Checked_Range_Boundary_Keeps_Neighbours()
        {
            var cache = CreateChecked("one two", (0, 3), (4, 3));

            // Append a new word: nothing existing is touched.
            Assert.True(cache.TryApplyEdit("one two", "one two x", trackEditedWord: true));

            Assert.Equal("one|two", Decorated(cache, "one two x"));
            Assert.Equal(" x", Unchecked(cache, "one two x"));
        }

        [Fact]
        public void Wholesale_Replacement_Drops_Everything_And_Rechecks_All()
        {
            var cache = CreateChecked("Ths sample", (0, 3));

            Assert.True(cache.TryApplyEdit("Ths sample", "Completely different", trackEditedWord: false));

            Assert.Empty(cache.Results);
            Assert.Equal("Completely different", Unchecked(cache, "Completely different"));
            Assert.Null(cache.LastEditedWord);
        }

        [Fact]
        public void Edit_Against_Unknown_Text_Fails_But_Still_Reports_The_Edited_Word()
        {
            var cache = new SpellCheckResultCache();

            Assert.False(cache.TryApplyEdit("T", "Th", trackEditedWord: true));

            Assert.Equal((0, 2), (cache.LastEditedWord!.Value.Start, cache.LastEditedWord.Value.End));
            Assert.Equal("Th", Unchecked(cache, "Th"));
        }

        [Fact]
        public void Programmatic_Edits_Do_Not_Track_The_Edited_Word()
        {
            var cache = CreateChecked("a wrod", (2, 4));

            Assert.True(cache.TryApplyEdit("a wrod", "a wrods", trackEditedWord: false));

            Assert.Null(cache.LastEditedWord);
        }

        [Fact]
        public void Supplementary_Letter_Remains_Part_Of_Edited_Word()
        {
            const string oldText = "foo\U00010400bar baz";
            const string newText = "foo\U00010400xbar baz";
            var cache = CreateChecked(oldText);

            Assert.True(cache.TryApplyEdit(oldText, newText, trackEditedWord: true));

            Assert.Equal(new SpellCheckRange(0, 9), cache.LastEditedWord);
            Assert.Equal("foo\U00010400xbar", Unchecked(cache, newText));
            Assert.Equal(0, SpellCheckResultCache.WordStart(newText, 6));
            Assert.Equal(9, SpellCheckResultCache.WordEnd(newText, 0));
        }

        [Theory]
        [InlineData("isn't", 4, "isn't")]
        [InlineData("wrd-wrd", 5, "wrd-wrd")]
        [InlineData("\U00010400bc", 3, "\U00010400bc")]
        [InlineData("wrng@example.com", 2, "wrng@example.com")]
        [InlineData("https://example.com/wrng", 22, "https://example.com/wrng")]
        [InlineData("lead,identifier_wrng,tail", 18, "identifier_wrng")]
        public void Context_Range_Uses_The_Same_Word_Boundaries_As_The_Result_Cache(
            string text,
            int caretIndex,
            string expected)
        {
            var ranges = new List<SpellCheckRange>();

            SpellCheckRangeFinder.AddContextRange(ranges, text, caretIndex, caretIndex, caretIndex);

            var range = Assert.Single(ranges);
            Assert.Equal(expected, text.Substring(range.Start, range.End - range.Start));
        }

        [Fact]
        public void Context_Range_Rejects_A_MultiToken_Selection()
        {
            const string text = "Ths wrng";
            var ranges = new List<SpellCheckRange>();

            SpellCheckRangeFinder.AddContextRange(ranges, text, 1, 0, text.Length);

            Assert.Empty(ranges);
        }

        [Fact]
        public void Context_Range_Allows_A_Selection_Within_One_Token()
        {
            const string text = "Ths wrng";
            var ranges = new List<SpellCheckRange>();

            SpellCheckRangeFinder.AddContextRange(ranges, text, 5, 5, 7);

            Assert.Equal(new SpellCheckRange(4, 8), Assert.Single(ranges));
        }

        [Theory]
        [InlineData("wrng", "wrng@example.com")]
        [InlineData("wrng@example.com", "wrng")]
        [InlineData("wrng", "wrng/path")]
        [InlineData("wrng.com", "wrng")]
        [InlineData("wrng", "identifier_wrng")]
        [InlineData("identifier_wrng", "wrng")]
        [InlineData("wrng", "wrng2")]
        public void Changing_Token_Eligibility_Invalidates_The_Whole_Token(string before, string after)
        {
            var oldText = "keep " + before + " tail";
            var newText = "keep " + after + " tail";
            var cache = CreateChecked(oldText, (0, 4), (5, before.Length), (6 + before.Length, 4));

            Assert.True(cache.TryApplyEdit(oldText, newText, trackEditedWord: false));

            Assert.Equal(after, Unchecked(cache, newText));
            Assert.Equal("keep|tail", Decorated(cache, newText));
        }

        [Theory]
        [InlineData(0, 4)] // The local part of an email.
        [InlineData(5, 16)] // A clipped domain fragment.
        public async Task Clipped_Address_Tokens_Are_Not_Sent_To_The_Provider(int start, int end)
        {
            const string text = "wrng@example.com";
            var provider = new RecordingSpellCheckProvider();
            var ranges = new List<SpellCheckRange>
            {
                new(start, end, startIsInsideWord: start > 0, endIsInsideWord: end < text.Length)
            };

            var results = await SpellChecker.CheckRangesAsync(text, ranges, provider, null, TestContext.Current.CancellationToken);

            Assert.Empty(provider.CheckedLengths);
            Assert.Empty(results);
        }

        [Fact]
        public async Task Oversized_Address_Token_Is_Skipped_Without_Checking_Fragments()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var text = new string('a', SpellChecker.MaxProviderCheckLength * 3) + "@example.com wrng";
                var provider = new RecordingSpellCheckProvider();
                var check = SpellChecker.CheckRangesAsync(text, new List<SpellCheckRange> { new(0, text.Length) },
                    provider, null, TestContext.Current.CancellationToken).AsTask();

                while (!check.IsCompleted)
                    Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);
                await check;

                Assert.Equal(" wrng", Assert.Single(provider.CheckedTexts));
            }
        }

        [Theory]
        [InlineData("hello—wrng—hello", 2, 13, "—wrng—", 5)]
        [InlineData("hello!wrng?hello", 2, 13, "!wrng?", 5)]
        [InlineData("你好世界wrng你好世界", 2, 10, "世界wrng你好", 2)]
        [InlineData("hello—wrng—hello", 0, 10, "hello—wrng", 0)]
        [InlineData("hello—isn't—hello", 2, 14, "—isn't—", 5)]
        [InlineData("hello—w\u0301rng—hello", 2, 14, "—w\u0301rng—", 5)]
        public async Task Clipped_Natural_Text_Checks_And_Caches_Complete_Unicode_Words(
            string text, int start, int end, string expected, int expectedStart)
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var ranges = new List<SpellCheckRange> { new(start, end, true, true) };
                var provider = new RecordingSpellCheckProvider();
                var check = SpellChecker.CheckRangesAsync(text, ranges, provider, null,
                    TestContext.Current.CancellationToken).AsTask();

                while (!check.IsCompleted)
                    Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

                var cache = new SpellCheckResultCache();
                cache.Set(text, ranges, await check, merge: false);

                Assert.Equal(expected, Assert.Single(provider.CheckedTexts));
                Assert.True(cache.AreRangesChecked(text,
                    new List<SpellCheckRange> { new(expectedStart, expectedStart + expected.Length) }));

                // A clipped edge word must not cause complete words to be checked again.
                provider.CheckedTexts.Clear();
                check = SpellChecker.CheckRangesAsync(text, cache.GetUncheckedRanges(text, ranges),
                    provider, null, TestContext.Current.CancellationToken).AsTask();
                while (!check.IsCompleted)
                    Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);
                await check;
                Assert.Empty(provider.CheckedTexts);
            }
        }

        [Theory]
        [InlineData("你好世界")]
        [InlineData("hello—wrng!")]
        [InlineData("\U00020000你好")]
        public async Task Long_Text_Without_Spaces_Is_Checked_In_Complete_Bounded_Chunks(string segment)
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var text = string.Concat(Enumerable.Repeat(segment, 2000));
                var provider = new RecordingSpellCheckProvider();
                var check = SpellChecker.CheckRangesAsync(text, new List<SpellCheckRange> { new(0, text.Length) },
                    provider, null, TestContext.Current.CancellationToken).AsTask();

                while (!check.IsCompleted)
                    Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);
                await check;

                Assert.Equal(text, string.Concat(provider.CheckedTexts));
                Assert.All(provider.CheckedTexts, chunk =>
                {
                    Assert.InRange(chunk.Length, 1, SpellChecker.MaxProviderCheckLength);
                    Assert.False(char.IsLowSurrogate(chunk[0]));
                    Assert.False(char.IsHighSurrogate(chunk[^1]));
                });
            }
        }

        [Theory]
        [InlineData("\U00010400", "\U00010401")] // Shared high surrogate.
        [InlineData("\U00010400", "\U00010800")] // Shared low surrogate.
        public void Replacing_A_Supplementary_Character_Invalidates_The_Whole_Scalar(
            string oldText,
            string newText)
        {
            var cache = CreateChecked(oldText);

            Assert.True(cache.TryApplyEdit(oldText, newText, trackEditedWord: true));

            var uncheckedRange = Assert.Single(cache.GetUncheckedRanges(
                newText,
                new List<SpellCheckRange> { new(0, newText.Length) }));
            Assert.Equal(new SpellCheckRange(0, newText.Length), uncheckedRange);
        }

        [Fact]
        public void Multiword_Edit_Tracks_Only_The_Word_At_The_Edit_End()
        {
            const string newText = "wrod anothr";
            var cache = CreateChecked(string.Empty);

            Assert.True(cache.TryApplyEdit(string.Empty, newText, trackEditedWord: true));

            Assert.Equal(new SpellCheckRange(5, 11), cache.LastEditedWord);
        }

        [Theory]
        [InlineData("bad good", "good")] // Delete the preceding word and separator.
        [InlineData("good", " good")] // Insert a separator before the existing word.
        [InlineData("badgood", "bad good")] // Split an existing word with a separator.
        public void Edit_Does_Not_Mark_The_Following_Untouched_Word_As_Edited(
            string oldText,
            string newText)
        {
            var cache = CreateChecked(oldText);

            Assert.True(cache.TryApplyEdit(oldText, newText, trackEditedWord: true));

            Assert.Null(cache.LastEditedWord);
        }

        [Fact]
        public void Deleting_An_Unchecked_Separator_Coalesces_Adjacent_Checked_Ranges()
        {
            const string oldText = "one  two";
            const string newText = "one two";
            var cache = CreateChecked(oldText);

            Assert.True(cache.TryApplyEdit(oldText, newText, trackEditedWord: true));

            var visible = new List<SpellCheckRange> { new(0, newText.Length) };
            Assert.True(cache.AreRangesChecked(newText, visible));
            Assert.Empty(cache.GetUncheckedRanges(newText, visible));
        }

        [Fact]
        public async Task Long_Provider_Checks_Are_Chunked_And_Yield_Between_Calls()
        {
            using (UnitTestApplication.Start(TestServices.MockPlatformRenderInterface))
            {
                var text = string.Join(' ', Enumerable.Repeat("word", 1200));
                var provider = new RecordingSpellCheckProvider();
                var ranges = new List<SpellCheckRange> { new(0, text.Length) };
                var check = SpellChecker.CheckRangesAsync(
                    text, ranges, provider, null, TestContext.Current.CancellationToken).AsTask();

                Assert.False(check.IsCompleted);

                while (!check.IsCompleted)
                {
                    Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);
                }

                await check;

                Assert.True(provider.CheckedLengths.Count > 1);
                Assert.All(provider.CheckedLengths, length => Assert.InRange(length, 1, SpellChecker.MaxProviderCheckLength));
            }
        }

        private sealed class RecordingSpellCheckProvider : ISpellCheckProvider
        {
            public List<int> CheckedLengths { get; } = new();
            public List<string> CheckedTexts { get; } = new();

            public bool IsLanguageSupported(CultureInfo? culture) => true;

            public ValueTask<IReadOnlyList<SpellCheckResult>> CheckAsync(
                ReadOnlyMemory<char> text,
                CultureInfo? culture,
                CancellationToken cancellationToken = default)
            {
                CheckedLengths.Add(text.Length);
                CheckedTexts.Add(text.ToString());
                return new ValueTask<IReadOnlyList<SpellCheckResult>>(Array.Empty<SpellCheckResult>());
            }

            public ValueTask<IReadOnlyList<string>> SuggestAsync(
                string word,
                CultureInfo? culture,
                CancellationToken cancellationToken = default)
            {
                return new ValueTask<IReadOnlyList<string>>(Array.Empty<string>());
            }
        }

        [Fact]
        public void Identical_Text_Is_A_No_Op()
        {
            var cache = CreateChecked("Ths sample", (0, 3));

            Assert.True(cache.TryApplyEdit("Ths sample", "Ths sample", trackEditedWord: true));

            Assert.Equal("Ths", Decorated(cache, "Ths sample"));
            Assert.Empty(cache.GetUncheckedRanges("Ths sample", new List<SpellCheckRange> { new(0, 10) }));
        }

        [Fact]
        public void Multiple_Edits_Accumulate_Unchecked_Holes_That_Merge_Back_On_Set()
        {
            var cache = CreateChecked("aaa bbb ccc", (0, 3));

            Assert.True(cache.TryApplyEdit("aaa bbb ccc", "aaa bbbx ccc", trackEditedWord: true));
            Assert.True(cache.TryApplyEdit("aaa bbbx ccc", "aaa bbbx cccx", trackEditedWord: true));

            var text = "aaa bbbx cccx";
            Assert.Equal("bbbx|cccx", Unchecked(cache, text));

            var holes = cache.GetUncheckedRanges(text, new List<SpellCheckRange> { new(0, text.Length) });
            cache.Set(text, holes, new[] { new SpellCheckResult(9, 4) }, merge: true);

            Assert.Equal("aaa|cccx", Decorated(cache, text));
            Assert.True(cache.AreRangesChecked(text, new List<SpellCheckRange> { new(0, text.Length) }));
        }

        [Fact]
        public void Unchecked_Ranges_Are_Widened_To_Word_Boundaries_Within_The_Viewport()
        {
            var cache = new SpellCheckResultCache();
            var text = "hello wonderful world";

            // Only "hello " has been checked so far.
            cache.Set(text, new List<SpellCheckRange> { new(0, 6) }, Array.Empty<SpellCheckResult>(), merge: false);

            // A viewport starting in the middle of "wonderful" must not check half a word.
            var visible = new List<SpellCheckRange> { new(9, text.Length) };
            var unchecked_ = cache.GetUncheckedRanges(text, visible);

            Assert.Equal(new[] { (9, 21) }, unchecked_.Select(r => (r.Start, r.End)));
        }

        [Fact]
        public void Clipped_Checks_Do_Not_Combine_Into_A_Falsely_Checked_Word()
        {
            const string text = "misspelled ok";
            var cache = new SpellCheckResultCache();

            cache.Set(
                text,
                new List<SpellCheckRange> { new(0, 5, endIsInsideWord: true) },
                Array.Empty<SpellCheckResult>(),
                merge: false);
            cache.Set(
                text,
                new List<SpellCheckRange> { new(5, text.Length, startIsInsideWord: true) },
                Array.Empty<SpellCheckResult>(),
                merge: true);

            var visible = new List<SpellCheckRange> { new(0, text.Length) };
            Assert.False(cache.AreRangesChecked(text, visible));
            Assert.Equal("misspelled", Unchecked(cache, text));
        }

        [Fact]
        public void Clipped_Check_Does_Not_Remove_An_Existing_Whole_Word_Result()
        {
            const string text = "misspelled ok";
            var cache = CreateChecked(text, (0, 10));

            cache.Set(
                text,
                new List<SpellCheckRange> { new(5, text.Length, startIsInsideWord: true) },
                Array.Empty<SpellCheckResult>(),
                merge: true);

            Assert.Equal("misspelled", Decorated(cache, text));
        }

        [Fact]
        public void Multiple_Visible_Ranges_Advance_Through_Checked_Ranges_Once()
        {
            const string text = "aaaa bbbb cccc dddd";
            var cache = new SpellCheckResultCache();
            var checkedRanges = new List<SpellCheckRange>
            {
                new(0, 5),
                new(10, 15)
            };

            cache.Set(text, checkedRanges, Array.Empty<SpellCheckResult>(), merge: false);

            Assert.True(cache.AreRangesChecked(text, checkedRanges));

            var uncheckedRanges = cache.GetUncheckedRanges(text, new List<SpellCheckRange>
            {
                new(0, 5),
                new(5, 10),
                new(10, 15),
                new(15, text.Length)
            });

            Assert.Equal(new[] { (5, 10), (15, text.Length) },
                uncheckedRanges.Select(range => (range.Start, range.End)));
        }

        [Fact]
        public void Misspelled_Word_Is_Extracted_Lazily_For_Suggestions()
        {
            var cache = CreateChecked("Ths sample", (0, 3));

            Assert.True(cache.TryGetMisspelledWord("Ths sample", caretIndex: 2, selectionStart: 2, selectionEnd: 2, out var result));
            Assert.Equal("Ths", result.Word);

            Assert.False(cache.TryGetMisspelledWord("Ths sample", caretIndex: 6, selectionStart: 6, selectionEnd: 6, out _));
        }
    }
}
