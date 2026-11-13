using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input.Platform;
using Avalonia.Threading;

namespace ControlCatalog.Controls
{
    /// <summary>
    /// The language a <see cref="CodeBlock"/> highlights.
    /// </summary>
    public enum CodeLanguage
    {
        Xaml,
        CSharp
    }

    /// <summary>
    /// A read-only, syntax-highlighted snippet with a copy button, collapsed until the reader asks for it.
    /// Highlighting is a small hand-rolled tokenizer rather than a dependency, and every colour comes from
    /// the catalog theme dictionaries so it reads in both variants.
    /// </summary>
    [TemplatePart(PART_Text, typeof(SelectableTextBlock))]
    [TemplatePart(PART_CopyButton, typeof(Button))]
    public partial class CodeBlock : TemplatedControl
    {
        private const string PART_Text = "PART_Text";
        private const string PART_CopyButton = "PART_CopyButton";

        public static readonly StyledProperty<string?> CodeProperty =
            AvaloniaProperty.Register<CodeBlock, string?>(nameof(Code));

        public static readonly StyledProperty<CodeLanguage> LanguageProperty =
            AvaloniaProperty.Register<CodeBlock, CodeLanguage>(nameof(Language));

        public static readonly StyledProperty<bool> IsExpandedProperty =
            AvaloniaProperty.Register<CodeBlock, bool>(nameof(IsExpanded), true);

        /// <summary>
        /// Snippets longer than this start collapsed. Catalog snippets are short by design (the median is
        /// four lines), so showing them costs less than a row that says nothing; only the rare long one
        /// is worth hiding.
        /// </summary>
        private const int CollapseThreshold = 12;

        private SelectableTextBlock? _text;
        private Button? _copyButton;
        private IDisposable? _copyReset;

        /// <summary>
        /// The snippet. Leading indentation common to every line is trimmed when it is rendered.
        /// </summary>
        public string? Code
        {
            get => GetValue(CodeProperty);
            set => SetValue(CodeProperty, value);
        }

        public CodeLanguage Language
        {
            get => GetValue(LanguageProperty);
            set => SetValue(LanguageProperty, value);
        }

        /// <summary>
        /// Whether the snippet is showing. Short snippets start open; ones longer than
        /// <see cref="CollapseThreshold"/> lines start collapsed.
        /// </summary>
        public bool IsExpanded
        {
            get => GetValue(IsExpandedProperty);
            set => SetValue(IsExpandedProperty, value);
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            if (_copyButton != null)
            {
                _copyButton.Click -= OnCopyClick;
            }

            _text = e.NameScope.Find<SelectableTextBlock>(PART_Text);
            _copyButton = e.NameScope.Find<Button>(PART_CopyButton);

            if (_copyButton != null)
            {
                _copyButton.Click += OnCopyClick;
            }

            UpdateInlines();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == CodeProperty)
            {
                var code = change.GetNewValue<string?>();
                if (!string.IsNullOrWhiteSpace(code))
                {
                    var lines = Dedent(code).Split('\n').Length;
                    SetCurrentValue(IsExpandedProperty, lines <= CollapseThreshold);
                }

                UpdateInlines();
            }
            else if (change.Property == LanguageProperty)
            {
                UpdateInlines();
            }
        }

        private async void OnCopyClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (TopLevel.GetTopLevel(this)?.Clipboard is not { } clipboard || Code is not { } code)
            {
                return;
            }

            await clipboard.SetTextAsync(Format(code));

            if (_copyButton is not { } button)
            {
                return;
            }

            button.Content = "Copied";
            _copyReset?.Dispose();
            _copyReset = DispatcherTimer.RunOnce(() => button.Content = "Copy", TimeSpan.FromSeconds(1.5));
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);

            _copyReset?.Dispose();
            _copyReset = null;

            if (_copyButton != null)
            {
                _copyButton.Content = "Copy";
            }
        }

        private void UpdateInlines()
        {
            if (_text is null)
            {
                return;
            }

            _text.Inlines?.Clear();

            if (string.IsNullOrWhiteSpace(Code))
            {
                return;
            }

            foreach (var (text, kind) in Tokenize(Format(Code), Language))
            {
                var run = new Run(text);
                if (kind is not null)
                {
                    run.Bind(Run.ForegroundProperty,
                        new Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension(kind));
                }

                _text.Inlines?.Add(run);
            }
        }

        private string Format(string code) =>
            Language == CodeLanguage.Xaml ? Realign(Dedent(code)) : Dedent(code);

        /// <summary>
        /// Removes the indentation every line shares, so a snippet written inside deeply nested XAML
        /// does not render with that nesting.
        /// </summary>
        internal static string Dedent(string code)
        {
            var lines = code.Replace("\r\n", "\n").Trim('\n').Split('\n');
            var common = int.MaxValue;

            foreach (var line in lines)
            {
                if (line.Trim().Length == 0)
                {
                    continue;
                }

                var indent = line.Length - line.TrimStart(' ').Length;
                common = Math.Min(common, indent);
            }

            if (common is 0 or int.MaxValue)
            {
                return string.Join("\n", lines).TrimEnd();
            }

            var builder = new StringBuilder();
            for (var i = 0; i < lines.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append('\n');
                }

                builder.Append(lines[i].Length >= common ? lines[i].Substring(common) : lines[i].TrimStart(' '));
            }

            return builder.ToString().TrimEnd();
        }

        [GeneratedRegex(@"^(\s*)<[\w:.]+\s+\S")]
        private static partial Regex OpeningTagRegex();

        [GeneratedRegex(@"^\s*([\w.:]+\s*=|/?>)")]
        private static partial Regex AttributeLineRegex();

        /// <summary>
        /// Aligns wrapped attributes under the first attribute of the element they belong to.
        /// Snippets are authored inside CDATA blocks, where the opening line sits at column zero but the
        /// continuation lines keep the indentation of the file they were written in, so the two no longer
        /// line up. Anything that is not a plain attribute continuation keeps the indentation it has.
        /// </summary>
        internal static string Realign(string code)
        {
            var lines = code.Split('\n');
            var anchor = -1;

            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                if (line.Trim().Length == 0)
                {
                    continue;
                }

                var opening = OpeningTagRegex().Match(line);
                if (opening.Success)
                {
                    // Column just past "<Tag ", measured on the line as it will be rendered.
                    var lead = opening.Groups[1].Value.Length;
                    var nameEnd = line.IndexOf(' ', lead);
                    anchor = nameEnd < 0 ? -1 : nameEnd + 1;
                    continue;
                }

                if (anchor >= 0 && AttributeLineRegex().IsMatch(line))
                {
                    lines[i] = new string(' ', anchor) + line.TrimStart(' ');
                    continue;
                }

                anchor = -1;
            }

            return string.Join("\n", lines);
        }

        private static readonly HashSet<string> s_csharpKeywords =
        [
            "abstract","as","async","await","base","bool","break","byte","case","catch","char","class","const",
            "continue","decimal","default","delegate","do","double","else","enum","event","explicit","false",
            "finally","float","for","foreach","get","if","implicit","in","int","interface","internal","is","lock",
            "long","namespace","new","null","object","operator","out","override","params","private","protected",
            "public","readonly","ref","return","sealed","set","short","static","string","struct","switch","this",
            "throw","true","try","typeof","uint","ulong","ushort","using","var","virtual","void","while","yield"
        ];

        /// <summary>
        /// Splits a snippet into spans paired with the theme resource key that colours them.
        /// A null key means the block's own foreground.
        /// </summary>
        internal static List<(string Text, string? BrushKey)> Tokenize(string code, CodeLanguage language) =>
            language == CodeLanguage.CSharp ? TokenizeCSharp(code) : TokenizeXaml(code);

        private static List<(string Text, string? BrushKey)> TokenizeXaml(string code)
        {
            var tokens = new List<(string Text, string? BrushKey)>();
            var i = 0;
            var run = new StringBuilder();

            void Flush()
            {
                if (run.Length > 0)
                {
                    tokens.Add((run.ToString(), null));
                    run.Clear();
                }
            }

            while (i < code.Length)
            {
                if (code.AsSpan(i).StartsWith("<!--"))
                {
                    Flush();
                    var end = code.IndexOf("-->", i, StringComparison.Ordinal);
                    end = end < 0 ? code.Length : end + 3;
                    tokens.Add((code[i..end], "CatalogCodeComment"));
                    i = end;
                    continue;
                }

                if (code[i] == '<')
                {
                    Flush();
                    var j = i + 1;
                    if (j < code.Length && code[j] == '/')
                    {
                        j++;
                    }

                    while (j < code.Length && (char.IsLetterOrDigit(code[j]) || code[j] is ':' or '.' or '_' or '-' or '/'))
                    {
                        j++;
                    }

                    tokens.Add((code[i..j], "CatalogCodeKeyword"));
                    i = j;
                    continue;
                }

                if (code[i] == '"')
                {
                    Flush();
                    var end = code.IndexOf('"', i + 1);
                    end = end < 0 ? code.Length : end + 1;
                    tokens.Add((code[i..end], "CatalogCodeString"));
                    i = end;
                    continue;
                }

                if (code[i] is '>' or '/' && (i + 1 >= code.Length || code[i] == '>' || code[i + 1] == '>'))
                {
                    Flush();
                    var len = code[i] == '/' && i + 1 < code.Length ? 2 : 1;
                    tokens.Add((code.Substring(i, len), "CatalogCodeKeyword"));
                    i += len;
                    continue;
                }

                if (char.IsLetter(code[i]) || code[i] == '_')
                {
                    var j = i;
                    while (j < code.Length && (char.IsLetterOrDigit(code[j]) || code[j] is '.' or ':' or '_' or '-'))
                    {
                        j++;
                    }

                    var word = code[i..j];
                    var k = j;
                    while (k < code.Length && code[k] == ' ')
                    {
                        k++;
                    }

                    if (k < code.Length && code[k] == '=')
                    {
                        Flush();
                        tokens.Add((word, "CatalogCodeAttribute"));
                        i = j;
                        continue;
                    }

                    run.Append(word);
                    i = j;
                    continue;
                }

                run.Append(code[i]);
                i++;
            }

            Flush();
            return tokens;
        }

        private static List<(string Text, string? BrushKey)> TokenizeCSharp(string code)
        {
            var tokens = new List<(string Text, string? BrushKey)>();
            var i = 0;
            var run = new StringBuilder();

            void Flush()
            {
                if (run.Length > 0)
                {
                    tokens.Add((run.ToString(), null));
                    run.Clear();
                }
            }

            while (i < code.Length)
            {
                if (code.AsSpan(i).StartsWith("//"))
                {
                    Flush();
                    var end = code.IndexOf('\n', i);
                    end = end < 0 ? code.Length : end;
                    tokens.Add((code[i..end], "CatalogCodeComment"));
                    i = end;
                    continue;
                }

                if (code[i] is '"' or '\'')
                {
                    Flush();
                    var quote = code[i];
                    var j = i + 1;
                    while (j < code.Length && code[j] != quote)
                    {
                        j += code[j] == '\\' ? 2 : 1;
                    }

                    j = Math.Min(j + 1, code.Length);
                    tokens.Add((code[i..j], "CatalogCodeString"));
                    i = j;
                    continue;
                }

                if (char.IsLetter(code[i]) || code[i] == '_')
                {
                    var j = i;
                    while (j < code.Length && (char.IsLetterOrDigit(code[j]) || code[j] == '_'))
                    {
                        j++;
                    }

                    var word = code[i..j];
                    if (s_csharpKeywords.Contains(word))
                    {
                        Flush();
                        tokens.Add((word, "CatalogCodeKeyword"));
                    }
                    else
                    {
                        run.Append(word);
                    }

                    i = j;
                    continue;
                }

                run.Append(code[i]);
                i++;
            }

            Flush();
            return tokens;
        }
    }
}
