using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.TextInput;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.LeakTests
{
    public class SpellCheckTests : ScopedTestBase
    {
        [ReleaseFact]
        public void Spell_Checked_TextBox_Is_Freed_When_Its_Window_Closes()
        {
            var provider = new Provider();

            using (Start(new DefaultPlatformSettings()))
            {
                static WeakReference Run(Provider provider)
                {
                    var textBox = new TextBox { Text = "Ths sample", ContextFlyout = null };
                    SpellCheck.SetIsEnabled(textBox, true);
                    SpellCheck.SetProvider(textBox, provider);

                    var window = new Window { Content = textBox };
                    window.Show();
                    window.LayoutManager.ExecuteInitialLayoutPass();

                    foreach (var timer in Dispatcher.SnapshotTimersForUnitTests())
                    {
                        timer.ForceFire();
                    }

                    // Exercise the suggestion path too.
                    textBox.CaretIndex = 1;
                    textBox.RaiseEvent(new ContextRequestedEventArgs());
                    Assert.NotEmpty(SpellCheck.GetSuggestions(textBox));

                    // Closing disposes the renderer, whose composition tree would otherwise keep the last
                    // rendered visuals alive in this test environment.
                    window.Close();

                    return new WeakReference(textBox);
                }

                var weakTextBox = Run(provider);
                Assert.True(weakTextBox.IsAlive);
                Assert.True(provider.ContextsCreated > 0);
                Assert.Equal(provider.ContextsCreated, provider.ContextsDisposed);

                CollectGarbage();

                Assert.False(weakTextBox.IsAlive);
            }
        }

        private static IDisposable Start(IPlatformSettings? settings)
        {
            static void Cleanup()
            {
                // KeyboardDevice holds a reference to the focused item.
                KeyboardDevice.Instance?.SetFocusedElement(null, NavigationMethod.Unspecified, KeyModifiers.None);
                Dispatcher.UIThread.RunJobs();
            }

            return new CompositeDisposable
            {
                Disposable.Create(Cleanup),
                UnitTestApplication.Start(TestServices.StyledWindow.With(
                    keyboardDevice: () => new KeyboardDevice(),
                    inputManager: new InputManager(),
                    accessKeyHandler: () => new AccessKeyHandler(),
                    platformSettings: settings))
            };
        }

        private static void CollectGarbage()
        {
            // Process all Loaded events to free control reference(s).
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);
            GC.Collect();

            Dispatcher.UIThread.RunJobs();
            GC.Collect();
        }

        private sealed class Provider : ISpellCheckProvider
        {
            public int ContextsCreated;
            public int ContextsDisposed;

            public IReadOnlyList<CultureInfo> SupportedCultures => Array.Empty<CultureInfo>();

            public ISpellCheckContext CreateContext(CultureInfo culture)
            {
                ContextsCreated++;
                return new Context(this);
            }

            private sealed class Context(Provider owner) : ISpellCheckContext
            {
                public ValueTask<IReadOnlyList<ISpellCheckResult>> CheckAsync(
                    ReadOnlyMemory<char> text,
                    CancellationToken cancellationToken = default) =>
                    new(text.Span.StartsWith("Ths") ? new ISpellCheckResult[] { new Result() } : Array.Empty<ISpellCheckResult>());

                public void Dispose() => owner.ContextsDisposed++;
            }

            private sealed class Result : ISpellCheckResult
            {
                public int Start => 0;
                public int Length => 3;
                public string? Description => null;

                public ValueTask<IReadOnlyList<string>> SuggestAsync(CancellationToken cancellationToken = default) =>
                    new(new[] { "This" });
            }
        }
    }
}
