using System.Threading.Tasks;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.FreeDesktop.AtSpi;
using Avalonia.FreeDesktop.AtSpi.Handlers;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests.Automation
{
    public class AtSpiTextHandlerTests : ScopedTestBase
    {
        private const uint WordStart = 1;
        private const uint WordEnd = 2;
        private const uint LineStart = 5;
        private const uint LineEnd = 6;
        private const uint GranularityWord = 1;
        private const uint GranularityLine = 3;

        [Fact]
        public async Task Offsets_Count_Code_Points()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);
            await using var server = new AtSpiServer();
            var (textBox, node, handler) = Create("a\U0001F600b", server);

            Assert.Equal(3, handler.CharacterCount);
            Assert.Equal("\U0001F600", await handler.GetTextAsync(1, 2));
            Assert.Equal(0x1F600, await handler.GetCharacterAtOffsetAsync(1));
            Assert.Equal('b', await handler.GetCharacterAtOffsetAsync(2));
            Assert.Equal(-1, await handler.GetCharacterAtOffsetAsync(3));

            // The caret after the emoji is three code units into the text but two characters.
            textBox.CaretIndex = 3;
            Assert.Equal(2, handler.CaretOffset);

            node.Detach();
        }

        [Fact]
        public async Task Line_Boundaries_Read_Whole_Lines()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);
            await using var server = new AtSpiServer();
            var (_, node, handler) = Create("one\ntwo", server);

            Assert.Equal(("one\n", 0, 4), await handler.GetTextAtOffsetAsync(1, LineStart));
            Assert.Equal(("two", 4, 7), await handler.GetTextAtOffsetAsync(5, LineStart));
            Assert.Equal(("\ntwo", 3, 7), await handler.GetTextAtOffsetAsync(5, LineEnd));
            Assert.Equal(("two", 4, 7), await handler.GetStringAtOffsetAsync(5, GranularityLine));

            node.Detach();
        }

        [Fact]
        public async Task Word_Boundaries_Follow_The_Start_And_End_Rules()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);
            await using var server = new AtSpiServer();
            var (_, node, handler) = Create("foo bar", server);

            // WORD_START runs from a word's start to the next word's start; WORD_END from the end of
            // the previous word to the end of this one.
            Assert.Equal(("foo ", 0, 4), await handler.GetTextAtOffsetAsync(1, WordStart));
            Assert.Equal(("foo", 0, 3), await handler.GetTextAtOffsetAsync(1, WordEnd));
            Assert.Equal((" bar", 3, 7), await handler.GetTextAtOffsetAsync(5, WordEnd));

            // The space after "foo" belongs to the following word's end range.
            Assert.Equal((" bar", 3, 7), await handler.GetTextAtOffsetAsync(3, WordEnd));

            Assert.Equal(("foo ", 0, 4), await handler.GetStringAtOffsetAsync(1, GranularityWord));
            Assert.Equal(("bar", 4, 7), await handler.GetTextAfterOffsetAsync(1, WordStart));
            Assert.Equal(("foo ", 0, 4), await handler.GetTextBeforeOffsetAsync(5, WordStart));
            Assert.Equal((string.Empty, 0, 0), await handler.GetTextAfterOffsetAsync(5, WordStart));

            node.Detach();
        }

        [Fact]
        public async Task A_Masked_Password_Reads_As_Its_Mask()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);
            await using var server = new AtSpiServer();
            var (_, node, handler) = Create("secret", server, passwordChar: '*');

            Assert.Equal(6, handler.CharacterCount);
            Assert.Equal("******", await handler.GetTextAsync(0, -1));
            Assert.Equal('*', await handler.GetCharacterAtOffsetAsync(0));

            node.Detach();
        }

        [Fact]
        public async Task Selection_Keeps_Its_Direction_And_Collapses_On_Removal()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);
            await using var server = new AtSpiServer();
            var (textBox, node, handler) = Create("Hello world", server);

            Assert.True(await handler.SetSelectionAsync(0, 5, 2));
            Assert.Equal(5, textBox.SelectionStart);
            Assert.Equal(2, textBox.SelectionEnd);
            Assert.Equal(2, handler.CaretOffset);
            Assert.Equal((2, 5), await handler.GetSelectionAsync(0));
            Assert.Equal(1, await handler.GetNSelectionsAsync());

            // Only one selection exists, and it cannot be added to while it is non-empty.
            Assert.False(await handler.SetSelectionAsync(1, 0, 1));
            Assert.False(await handler.AddSelectionAsync(0, 1));

            Assert.True(await handler.RemoveSelectionAsync(0));
            Assert.Equal(0, await handler.GetNSelectionsAsync());
            Assert.Equal(2, handler.CaretOffset);

            Assert.False(await handler.SetSelectionAsync(0, 0, 100));
            Assert.False(await handler.SetCaretOffsetAsync(-1));

            node.Detach();
        }

        [Fact]
        public async Task Attributes_Report_Defaults_Separately_And_Sizes_In_Points()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);
            await using var server = new AtSpiServer();
            var (textBox, node, handler) = Create("foo", server);
            textBox.FontSize = 16;

            var defaults = await handler.GetDefaultAttributesAsync();
            Assert.Equal("12", defaults["size"]);

            // A uniform document differs from its defaults nowhere.
            var (attributes, start, end) = await handler.GetAttributesAsync(1);
            Assert.Empty(attributes);
            Assert.Equal((0, 3), (start, end));

            var (withDefaults, _, _) = await handler.GetAttributeRunAsync(1, includeDefaults: true);
            Assert.Equal("12", withDefaults["size"]);

            node.Detach();
        }

        [Fact]
        public async Task EditableText_Refuses_To_Rebuild_A_Masked_Password()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);
            await using var server = new AtSpiServer();
            var (textBox, node, _) = Create("secret", server, passwordChar: '*');
            var editable = new AtSpiEditableTextHandler(node);

            Assert.False(await editable.InsertTextAsync(0, "x", 1));
            Assert.False(await editable.DeleteTextAsync(0, 1));
            Assert.Equal("secret", textBox.Text);

            node.Detach();
        }

        [Fact]
        public async Task EditableText_Edits_At_Character_Offsets()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);
            await using var server = new AtSpiServer();
            var (textBox, node, _) = Create("a\U0001F600b", server);
            var editable = new AtSpiEditableTextHandler(node);

            // Character 2 is "b", which sits after the two code units of the emoji.
            Assert.True(await editable.InsertTextAsync(2, "X", 1));
            Assert.Equal("a\U0001F600Xb", textBox.Text);

            Assert.True(await editable.DeleteTextAsync(1, 2));
            Assert.Equal("aXb", textBox.Text);

            node.Detach();
        }

        [Fact]
        public void TextModel_Reports_Replacements_In_Characters()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);
            var textBox = new TextBox { Text = "\U0001F600hello" };
            var model = new AtSpiTextModel(Accessible(textBox));
            model.RecordBaseline();

            textBox.Text = "\U0001F600help";

            Assert.Equal((4, "lo", "p"), model.TakeChange());
            Assert.Null(model.TakeChange());
        }

        [Fact]
        public void TextModel_Never_Splits_A_Surrogate_Pair_In_A_Change()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

            // The two emoji share their high surrogate, so a code unit diff would start mid-pair.
            var textBox = new TextBox { Text = "a\U0001F600" };
            var model = new AtSpiTextModel(Accessible(textBox));
            model.RecordBaseline();

            textBox.Text = "a\U0001F601";

            Assert.Equal((1, "\U0001F600", "\U0001F601"), model.TakeChange());
        }

        [Fact]
        public async Task Detaching_The_Node_Releases_Its_Text_Model()
        {
            using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);
            await using var server = new AtSpiServer();
            var (_, node, _) = Create("text", server);

            Assert.NotNull(node.TextModel);
            node.Detach();
            Assert.Null(node.TextModel);
        }

        private static (TextBox TextBox, AtSpiNode Node, AtSpiAccessibleTextHandler Handler) Create(
            string text, AtSpiServer server, char passwordChar = default)
        {
            var textBox = new TextBox { Text = text, PasswordChar = passwordChar };
            var node = server.GetOrCreateNode(ControlAutomationPeer.CreatePeerForElement(textBox));
            node.Attach(null);
            return (textBox, node, new AtSpiAccessibleTextHandler(node, node.TextModel!));
        }

        private static IAccessibleText Accessible(TextBox textBox)
            => ControlAutomationPeer.CreatePeerForElement(textBox).GetProvider<IAccessibleText>()!;
    }
}
