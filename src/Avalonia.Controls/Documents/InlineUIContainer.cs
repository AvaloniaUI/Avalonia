using System.Collections.Generic;
using System.Text;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Metadata;

namespace Avalonia.Controls.Documents
{
    /// <summary>
    /// InlineUIContainer - a wrapper for embedded UIElements in text 
    /// flow content inline collections
    /// </summary>
    public class InlineUIContainer : Inline
    {
        /// <summary>
        /// Defines the <see cref="Child"/> property.
        /// </summary>
        public static readonly StyledProperty<Control> ChildProperty =
            AvaloniaProperty.Register<InlineUIContainer, Control>(nameof(Child));

        static InlineUIContainer()
        {
            BaselineAlignmentProperty.OverrideDefaultValue<InlineUIContainer>(BaselineAlignment.Top);
        }

        /// <summary>
        /// Initializes a new instance of InlineUIContainer element.
        /// </summary>
        /// <remarks>
        /// The purpose of this element is to be a wrapper for UIElements
        /// when they are embedded into text flow - as items of
        /// InlineCollections.
        /// </remarks>
        public InlineUIContainer()
        {
        }

        /// <summary>
        /// Initializes an InlineBox specifying its child UIElement
        /// </summary>
        /// <param name="child">
        /// UIElement set as a child of this inline item
        /// </param>
        public InlineUIContainer(Control child)
        {
            Child = child;
        }

        /// <summary>
        /// The content spanned by this TextElement.
        /// </summary>
        [Content]
        public Control Child
        {
            get => GetValue(ChildProperty);
            set => SetValue(ChildProperty, value);
        }

        internal override void BuildTextRun(IList<TextRun> textRuns)
        {
            if(!Child.IsMeasureValid)
            {
                Child.Measure(Size.Infinity);
            }

            textRuns.Add(new EmbeddedControlRun(Child, CreateTextRunProperties()));
        }

        internal override void AppendText(StringBuilder stringBuilder)
        {
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == ChildProperty)
            {
                if(change.OldValue is Control oldChild)
                {
                    LogicalChildren.Remove(oldChild);
                }

                if(change.NewValue is Control newChild)
                {
                    LogicalChildren.Add(newChild);
                }
            }
        }
    }
}
