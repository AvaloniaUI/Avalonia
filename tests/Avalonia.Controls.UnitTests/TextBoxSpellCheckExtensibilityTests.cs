using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Harfbuzz;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Input.TextInput;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Avalonia.Threading;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class TextBoxSpellCheckExtensibilityTests : ScopedTestBase
    {
        [Fact]
        public void Registered_Hunspell_Style_Provider_Replaces_Native_Platform_Provider()
        {
            using (UnitTestApplication.Start(Services))
            {
                var hunspellProvider = new HunspellStyleSpellCheckProvider();
                var nativeProvider = new TestSpellCheckProvider("NativeSuggestion");

                AvaloniaLocator.CurrentMutable.Bind<ISpellCheckProvider>().ToConstant(hunspellProvider);

                var target = CreateTextBoxInTopLevel("teh sample", nativeProvider);
                target.CaretIndex = 1;

                target.RaiseEvent(new ContextRequestedEventArgs());
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

                Assert.Equal(new[] { "the" }, target.SpellCheckSuggestions);
                Assert.Equal(1, hunspellProvider.CheckCount);
                Assert.Equal(0, nativeProvider.CheckCount);
            }
        }

        [Fact]
        public void Inherited_Spell_Check_Provider_Replaces_Native_Platform_Provider()
        {
            using (UnitTestApplication.Start(Services))
            {
                var hunspellProvider = new HunspellStyleSpellCheckProvider();
                var nativeProvider = new TestSpellCheckProvider("NativeSuggestion");

                var target = CreateTextBoxInTopLevel(
                    "teh sample",
                    nativeProvider,
                    topLevel => TextInputOptions.SetSpellCheckProvider(topLevel, hunspellProvider));
                target.CaretIndex = 1;

                target.RaiseEvent(new ContextRequestedEventArgs());
                Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

                Assert.Equal(new[] { "the" }, target.SpellCheckSuggestions);
                Assert.Equal(1, hunspellProvider.CheckCount);
                Assert.Equal(0, nativeProvider.CheckCount);
            }
        }

        private static TestServices Services => TestServices.MockThreadingInterface.With(
            standardCursorFactory: Mock.Of<ICursorFactory>(),
            renderInterface: new HeadlessPlatformRenderInterface(),
            textShaperImpl: new HarfBuzzTextShaper(),
            fontManagerImpl: new TestFontManager(),
            assetLoader: new StandardAssetLoader());

        private static TextBox CreateTextBoxInTopLevel(
            string text,
            ISpellCheckProvider platformProvider,
            Action<TestTopLevel>? configureTopLevel = null)
        {
            var target = new TextBox
            {
                Template = TextBoxTests.CreateTemplate(),
                Text = text
            };

            var topLevel = new TestTopLevel(CreateMockTopLevelImpl(platformProvider).Object)
            {
                Template = CreateTopLevelTemplate(),
                Content = target
            };

            configureTopLevel?.Invoke(topLevel);

            topLevel.ApplyTemplate();
            topLevel.LayoutManager.ExecuteInitialLayoutPass();

            return target;
        }

        private static Mock<ITopLevelImpl> CreateMockTopLevelImpl(ISpellCheckProvider spellCheckProvider)
        {
            var impl = new Mock<ITopLevelImpl>();
            impl.Setup(x => x.Compositor).Returns(RendererMocks.CreateDummyCompositor());
            impl.Setup(r => r.TryGetFeature(typeof(IClipboard)))
                .Returns(new Clipboard(new HeadlessClipboardImplStub()));
            impl.Setup(r => r.TryGetFeature(typeof(ISpellCheckProvider)))
                .Returns(spellCheckProvider);
            impl.SetupGet(x => x.RenderScaling).Returns(1);
            return impl;
        }

        private static FuncControlTemplate<TestTopLevel> CreateTopLevelTemplate()
        {
            return new FuncControlTemplate<TestTopLevel>((x, scope) =>
                new ContentPresenter
                {
                    Name = "PART_ContentPresenter",
                    [!ContentPresenter.ContentProperty] = x[!ContentControl.ContentProperty],
                }.RegisterInNameScope(scope));
        }

        private sealed class TestTopLevel(ITopLevelImpl impl) : TopLevel(impl)
        {
        }

        private sealed class HunspellStyleSpellCheckProvider : ISpellCheckProvider
        {
            public int CheckCount { get; private set; }

            public bool IsLanguageSupported(CultureInfo? culture) => true;

            public ValueTask<IReadOnlyList<SpellCheckResult>> CheckAsync(
                string text,
                CultureInfo? culture,
                CancellationToken cancellationToken = default)
            {
                CheckCount++;

                var index = text.IndexOf("teh", StringComparison.Ordinal);

                return new ValueTask<IReadOnlyList<SpellCheckResult>>(
                    index < 0
                        ? Array.Empty<SpellCheckResult>()
                        : new[] { new SpellCheckResult(index, 3, "teh") });
            }

            public ValueTask<IReadOnlyList<string>> SuggestAsync(
                string word,
                CultureInfo? culture,
                CancellationToken cancellationToken = default)
            {
                return new ValueTask<IReadOnlyList<string>>(new[] { "the" });
            }
        }

        private sealed class TestSpellCheckProvider(string suggestion) : ISpellCheckProvider
        {
            public int CheckCount { get; private set; }

            public bool IsLanguageSupported(CultureInfo? culture) => true;

            public ValueTask<IReadOnlyList<SpellCheckResult>> CheckAsync(
                string text,
                CultureInfo? culture,
                CancellationToken cancellationToken = default)
            {
                CheckCount++;
                return new ValueTask<IReadOnlyList<SpellCheckResult>>(
                    new[] { new SpellCheckResult(0, text.Length, text) });
            }

            public ValueTask<IReadOnlyList<string>> SuggestAsync(
                string word,
                CultureInfo? culture,
                CancellationToken cancellationToken = default)
            {
                return new ValueTask<IReadOnlyList<string>>(new[] { suggestion });
            }
        }
    }
}
