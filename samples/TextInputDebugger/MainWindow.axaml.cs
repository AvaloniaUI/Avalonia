using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Input.TextInput;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Threading;

namespace TextInputDebugger
{
    public partial class MainWindow : Window
    {
        private const int MaxEntries = 8000;

        private readonly ConditionalWeakTable<TextInputMethodClient, StructuredTextInputRecorder> _recorders = new();
        private readonly List<TraceEntry> _allEntries = new();
        private readonly ObservableCollection<TraceEntry> _visibleEntries = new();

        private StructuredTextInputRecorder? _active;
        private int _sequence;

        public MainWindow()
        {
            InitializeComponent();

            LogList.ItemsSource = _visibleEntries;

            // Runs in the bubble phase, after the control's class handler has supplied its
            // client, so the platform backend receives the recorder instead. The weak table
            // keeps recorder identity stable across the manager's re-queries - a fresh
            // wrapper per query would look like a client swap to the backend.
            AddHandler(InputElement.TextInputMethodClientRequestedEvent, OnClientRequested, RoutingStrategies.Bubble);

            FilterReads.IsCheckedChanged += (_, _) => RebuildVisibleEntries();
            FilterGeometry.IsCheckedChanged += (_, _) => RebuildVisibleEntries();
            FilterLegacy.IsCheckedChanged += (_, _) => RebuildVisibleEntries();
            ClearLogButton.Click += (_, _) => { _allEntries.Clear(); _visibleEntries.Clear(); };

            ComposeButton.Click += (_, _) => WithActive(r => r.SetCompositionText("かん", 2));
            UpdateCompositionButton.Click += (_, _) => WithActive(r => r.SetCompositionText("感", 1));
            DecorateButton.Click += (_, _) => WithActive(DecorateComposition);
            CommitButton.Click += (_, _) => WithActive(r => r.CommitComposition());
            ReplaceSelectionButton.Click += (_, _) => WithActive(r =>
            {
                var structured = (IStructuredTextInput)r;
                r.ReplaceText(structured.Selection, "x");
            });

            UpdateState();
        }

        private void OnClientRequested(object? sender, TextInputMethodClientRequestedEventArgs e)
        {
            if (e.Client is IStructuredTextInput && e.Client is not StructuredTextInputRecorder)
            {
                var recorder = _recorders.GetValue(e.Client, CreateRecorder);
                _active = recorder;
                e.Client = recorder;
                UpdateState();
            }
        }

        private StructuredTextInputRecorder CreateRecorder(TextInputMethodClient inner)
        {
            var recorder = new StructuredTextInputRecorder(inner, AddEntry);
            recorder.TextChanged += (_, _) => OnClientStateChanged();
            ((IStructuredTextInput)recorder).CaretPositionChanged += (_, _) => OnClientStateChanged();
            ((IStructuredTextInput)recorder).CompositionChanged += (_, _) => OnClientStateChanged();
            ((IStructuredTextInput)recorder).InputDecorationsChanged += (_, _) => OnClientStateChanged();
            recorder.SelectionChanged += (_, _) => OnClientStateChanged();
            return recorder;
        }

        private void AddEntry(TraceCategory category, string member, string details, bool isError)
        {
            void Append()
            {
                var entry = new TraceEntry
                {
                    Seq = ++_sequence,
                    Time = DateTime.Now.ToString("HH:mm:ss.fff"),
                    Category = category,
                    Member = member,
                    Details = details,
                    IsError = isError,
                };

                _allEntries.Add(entry);
                if (_allEntries.Count > MaxEntries)
                {
                    _allEntries.RemoveRange(0, MaxEntries / 2);
                    RebuildVisibleEntries();
                }
                else if (PassesFilter(entry))
                {
                    _visibleEntries.Add(entry);
                    LogList.ScrollIntoView(entry);
                }
            }

            // Always deferred, even on the UI thread: appending scrolls the log, and a
            // synchronous layout pass from inside a client callback re-enters the text
            // stack mid-update (this is exactly how the first live IME session crashed
            // the preedit path). Posted closures run in order, so the trace stays sequential.
            Dispatcher.UIThread.Post(Append);
        }

        private bool PassesFilter(TraceEntry entry) => entry.Category switch
        {
            TraceCategory.Read => FilterReads.IsChecked == true,
            TraceCategory.Geometry => FilterGeometry.IsChecked == true,
            TraceCategory.Legacy => FilterLegacy.IsChecked == true,
            _ => true,
        };

        private void RebuildVisibleEntries()
        {
            _visibleEntries.Clear();
            foreach (var entry in _allEntries)
            {
                if (PassesFilter(entry))
                {
                    _visibleEntries.Add(entry);
                }
            }

            if (_visibleEntries.Count > 0)
            {
                LogList.ScrollIntoView(_visibleEntries[^1]);
            }
        }

        private void WithActive(Action<StructuredTextInputRecorder> action)
        {
            if (_active is null)
            {
                AddEntry(TraceCategory.Invariant, "Simulator", "no structured client attached - focus the text box first", true);
                return;
            }

            action(_active);
        }

        private void DecorateComposition(StructuredTextInputRecorder recorder)
        {
            // Probe reads go through the unwrapped client; only the decoration call itself
            // should appear in the trace, the way a backend would produce it.
            var structured = recorder.Inner;
            if (structured.CompositionRange is not { } composition)
            {
                AddEntry(TraceCategory.Invariant, "Simulator", "no composition to decorate - press Compose first", true);
                return;
            }

            var start = composition.Start.Offset;
            var end = composition.End.Offset;
            if (end - start < 2)
            {
                recorder.SetInputDecorations(new[]
                {
                    new TextInputDecoration(composition, TextInputDecorationKind.ConvertedTarget),
                });
                return;
            }

            var middle = structured.GetPosition(composition.Start, (end - start) / 2);
            recorder.SetInputDecorations(new[]
            {
                new TextInputDecoration(structured.GetRange(composition.Start, middle), TextInputDecorationKind.ConvertedTarget),
                new TextInputDecoration(structured.GetRange(middle, composition.End), TextInputDecorationKind.Input),
            });
        }

        private void OnClientStateChanged()
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                UpdateState();
            }
            else
            {
                Dispatcher.UIThread.Post(UpdateState);
            }
        }

        private void UpdateState()
        {
            if (_active is null)
            {
                StateClient.Text = "client: none (focus the text box)";
                StateVersion.Text = StateCaret.Text = StateSelection.Text = StateComposition.Text = StateDecorations.Text = "";
                DocumentView.Inlines?.Clear();
                return;
            }

            // Read through the unwrapped client so the state panel's own probes do not
            // pollute the trace with reads the backend never made.
            var structured = _active.Inner;
            var text = structured.GetTextUnlogged();
            var caret = structured.CaretPosition;
            var selection = structured.Selection;
            var composition = structured.CompositionRange;
            var decorations = structured.InputDecorations;

            StateClient.Text = $"client: {_active.Inner.GetType().Name}  preedit={_active.SupportsPreedit}  inDoc={_active.SupportsInDocumentComposition}";
            StateVersion.Text = $"version: {structured.DocumentVersion}  length: {text.Length}";
            StateCaret.Text = $"caret: {caret.Offset} ({(caret.Gravity == LogicalDirection.Forward ? "forward" : "backward")})";
            StateSelection.Text = $"selection: [{selection.Start.Offset}..{selection.End.Offset})";
            StateComposition.Text = composition is null
                ? "composition: none"
                : $"composition: [{composition.Start.Offset}..{composition.End.Offset})";

            if (decorations.Count == 0)
            {
                StateDecorations.Text = "decorations: none";
            }
            else
            {
                var parts = new List<string>();
                foreach (var decoration in decorations)
                {
                    parts.Add($"{decoration.Kind}[{decoration.Range.Start.Offset}..{decoration.Range.End.Offset})");
                }

                StateDecorations.Text = "decorations: " + string.Join(" ", parts);
            }

            RebuildDocumentView(text, caret.Offset, selection.Start.Offset, selection.End.Offset,
                composition?.Start.Offset, composition?.End.Offset);
        }

        private void RebuildDocumentView(string text, int caret, int selStart, int selEnd, int? compStart, int? compEnd)
        {
            var inlines = DocumentView.Inlines ??= new InlineCollection();
            inlines.Clear();

            var boundaries = new SortedSet<int> { 0, text.Length, caret };
            boundaries.Add(Math.Clamp(selStart, 0, text.Length));
            boundaries.Add(Math.Clamp(selEnd, 0, text.Length));
            if (compStart.HasValue)
            {
                boundaries.Add(Math.Clamp(compStart.Value, 0, text.Length));
                boundaries.Add(Math.Clamp(compEnd!.Value, 0, text.Length));
            }

            var edges = new List<int>(boundaries);
            for (var i = 0; i < edges.Count; i++)
            {
                var offset = edges[i];
                if (offset == caret)
                {
                    inlines.Add(new Run("|") { Foreground = Brushes.Red, FontWeight = FontWeight.Bold });
                }

                if (i == edges.Count - 1)
                {
                    break;
                }

                var next = edges[i + 1];
                if (next <= offset)
                {
                    continue;
                }

                var run = new Run(text[offset..next].Replace("\r", "\\r").Replace("\n", "\\n\n"));
                var inSelection = offset >= selStart && next <= selEnd && selStart != selEnd;
                var inComposition = compStart.HasValue && offset >= compStart.Value && next <= compEnd!.Value;
                if (inComposition)
                {
                    run.Background = Brushes.Plum;
                    run.TextDecorations = TextDecorations.Underline;
                }
                else if (inSelection)
                {
                    run.Background = Brushes.LightSteelBlue;
                }

                inlines.Add(run);
            }
        }
    }
}
