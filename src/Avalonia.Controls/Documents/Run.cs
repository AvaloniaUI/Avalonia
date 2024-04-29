using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Data;
using Avalonia.Media.TextFormatting;
using Avalonia.Metadata;

namespace Avalonia.Controls.Documents
{
    /// <summary>
    /// A terminal element in text flow hierarchy - contains a uniformatted run of unicode characters
    /// </summary>
    public class Run : Inline
    {
        /// <summary>
        /// Initializes an instance of Run class.
        /// </summary>
        public Run()
        {
        }

        /// <summary>
        /// Initializes an instance of Run class specifying its text content.
        /// </summary>
        /// <param name="text">
        /// Text content assigned to the Run.
        /// </param>
        public Run(string? text)
        {
            Text = text;
        }

        /// <summary>
        /// Dependency property backing Text.
        /// </summary>
        /// <remarks>
        /// Note that when a TextRange that intersects with this Run gets modified (e.g. by editing 
        /// a selection in RichTextBox), we will get two changes to this property since we delete 
        /// and then insert when setting the content of a TextRange.
        /// </remarks>
        public static readonly StyledProperty<string?> TextProperty = AvaloniaProperty.Register<Run, string?> (
            nameof (Text), defaultBindingMode: BindingMode.TwoWay);

        /// <summary>
        /// The content spanned by this TextElement.
        /// </summary>
        [Content]
        public string? Text {
            get => GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        internal override void BuildTextRun(IList<TextRun> textRuns)
        {
            var text = Text ?? "";

            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            var textRunProperties = CreateTextRunProperties();           

            var textCharacters = new TextCharacters(text, textRunProperties);

            textRuns.Add(textCharacters);
        }

        internal override void AppendText(StringBuilder stringBuilder)
        {
            var text = Text ?? "";

            stringBuilder.Append(text);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            switch (change.Property.Name)
            {
                case nameof(Text):
                    InlineHost?.Invalidate();
                    break;
            }
        }
    }
}
