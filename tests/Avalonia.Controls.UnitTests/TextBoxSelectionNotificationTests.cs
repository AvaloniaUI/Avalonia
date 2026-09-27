using System.Collections.Generic;
using Avalonia.Harfbuzz;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.TextInput;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class TextBoxSelectionNotificationTests : ScopedTestBase
    {
        [Theory]
        [InlineData(8, 18, 5, 5)]
        [InlineData(18, 8, 5, 5)]
        [InlineData(2, 18, 2, 5)]
        [InlineData(18, 2, 5, 2)]
        public void Replacing_Text_Notifies_Input_Method_After_Both_Selection_Endpoints_Are_Coerced(
            int start, int end, int expectedStart, int expectedEnd)
        {
            using var app = UnitTestApplication.Start(Services);
            var textBox = CreateTextBox();
            var client = GetClient(textBox);
            client.Selection = new TextSelection(start, end);
            Assert.Equal(0, textBox.CaretIndex);

            var selections = new List<TextSelection>();
            client.SelectionChanged += (_, _) => selections.Add(client.Selection);

            textBox.Text = "short";

            Assert.Equal(new TextSelection(expectedStart, expectedEnd), Assert.Single(selections));
            Assert.Equal("short", textBox.Text);
        }

        [Theory]
        [InlineData(8, 18)]
        [InlineData(18, 8)]
        public void Replacing_Text_Allows_Reentrant_Input_From_Input_Method_Selection_Changed(
            int start, int end)
        {
            using var app = UnitTestApplication.Start(Services);
            var textBox = CreateTextBox();
            var client = GetClient(textBox);
            client.Selection = new TextSelection(start, end);
            Assert.Equal(0, textBox.CaretIndex);

            var inserted = false;
            client.SelectionChanged += (_, _) =>
            {
                if (inserted)
                    return;

                inserted = true;
                textBox.RaiseEvent(new TextInputEventArgs
                {
                    RoutedEvent = InputElement.TextInputEvent,
                    Text = "!"
                });
            };

            textBox.Text = "short";

            Assert.True(inserted);
            Assert.Equal("short!", textBox.Text);
            Assert.Equal(new TextSelection(6, 6), client.Selection);
            Assert.Equal(6, textBox.CaretIndex);
        }

        [Fact]
        public void Replacing_Text_Preserves_Valid_Partial_Selection_Without_Notifying_Input_Method()
        {
            using var app = UnitTestApplication.Start(Services);
            var textBox = CreateTextBox();
            var client = GetClient(textBox);
            client.Selection = new TextSelection(2, 4);
            var notifications = 0;
            client.SelectionChanged += (_, _) => ++notifications;

            textBox.Text = "short";

            Assert.Equal(new TextSelection(2, 4), client.Selection);
            Assert.Equal("or", textBox.SelectedText);
            Assert.Equal(0, notifications);
        }

        private static TestServices Services => TestServices.MockThreadingInterface.With(
            standardCursorFactory: Mock.Of<ICursorFactory>(),
            renderInterface: new HeadlessPlatformRenderInterface(),
            textShaperImpl: new HarfBuzzTextShaper(),
            fontManagerImpl: new TestFontManager(),
            assetLoader: new StandardAssetLoader());

        private static TextBox CreateTextBox()
        {
            var textBox = new TextBox
            {
                Template = TextBoxTests.CreateTemplate(),
                Text = "abcdefghijklmnopqrst",
                CaretIndex = 0
            };
            textBox.ApplyTemplate();
            return textBox;
        }

        private static TextInputMethodClient GetClient(TextBox textBox)
        {
            var args = new TextInputMethodClientRequestedEventArgs
            {
                RoutedEvent = InputElement.TextInputMethodClientRequestedEvent
            };
            textBox.RaiseEvent(args);
            Assert.NotNull(args.Client);
            return args.Client;
        }
    }
}
