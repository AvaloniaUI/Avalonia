using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Input.TextInput;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class TextBoxSpellCheckFlyoutTests : ScopedTestBase
    {
        [Fact]
        public void Applying_A_Suggestion_Closes_The_Context_Flyout()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow.With(
                windowingPlatform: new MockWindowingPlatform(null, x => MockWindowingPlatform.CreatePopupMock(x).Object),
                keyboardDevice: () => new KeyboardDevice())))
            {
                var flyout = new MenuFlyout();
                var target = new TextBox { Text = "Ths sample", ContextFlyout = flyout };
                SpellCheck.SetIsEnabled(target, true);
                SpellCheck.SetProvider(target, new Provider());

                var platform = AvaloniaLocator.Current.GetRequiredService<IWindowingPlatform>();
                var windowImpl = Mock.Get(platform.CreateWindow());
                windowImpl.Setup(x => x.Compositor).Returns(RendererMocks.CreateDummyCompositor());
                var window = new Window(windowImpl.Object) { Content = target };
                window.ApplyTemplate();
                window.Show();

                target.CaretIndex = 1;
                target.RaiseEvent(new ContextRequestedEventArgs());
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

                Assert.True(flyout.IsOpen);
                Assert.Equal(new[] { "This" }, SpellCheck.GetSuggestions(target));

                target.ApplySpellCheckSuggestion("This");

                Assert.Equal("This sample", target.Text);
                Assert.False(flyout.IsOpen);
            }
        }

        private sealed class Provider : ISpellCheckProvider
        {
            public IReadOnlyList<CultureInfo> SupportedCultures => Array.Empty<CultureInfo>();

            public ISpellCheckContext CreateContext(CultureInfo culture) => new Context();

            private sealed class Context : SpellCheckContextBase
            {
                protected override ValueTask<IReadOnlyList<ISpellCheckResult>> CheckCoreAsync(
                    ReadOnlyMemory<char> text,
                    CancellationToken cancellationToken) =>
                    new(text.Span.StartsWith("Ths")
                        ? new ISpellCheckResult[] { new TestSpellCheckResult(0, 3, "Ths", new[] { "This" }) }
                        : Array.Empty<ISpellCheckResult>());
            }
        }
    }
}
