using System.Collections.Generic;
using System.Text;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Metadata;
using Avalonia.Utilities;

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
        public static readonly StyledProperty<IControl> ChildProperty =
            AvaloniaProperty.Register<InlineUIContainer, IControl>(nameof(Child));

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
        public InlineUIContainer(IControl child)
        {
            Child = child;
        }

        /// <summary>
        /// The content spanned by this TextElement.
        /// </summary>
        [Content]
        public IControl Child
        {
            get => GetValue(ChildProperty);
            set => SetValue(ChildProperty, value);
        }

        internal override void BuildTextRun(IList<TextRun> textRuns)
        {
            if(InlineHost == null)
            {
                return;
            }

            ((ISetLogicalParent)Child).SetParent(InlineHost);

            InlineHost.AddVisualChild(Child);

            textRuns.Add(new InlineRun(Child, CreateTextRunProperties()));
        }

        internal override void AppendText(StringBuilder stringBuilder)
        {
        }

        private class InlineRun : DrawableTextRun
        {
            public InlineRun(IControl control, TextRunProperties properties)
            {
                Control = control;
                Properties = properties;
            }

            public IControl Control { get; }

            public override TextRunProperties? Properties { get; }

            public override Size Size => Control.DesiredSize;

            public override double Baseline
            {
                get
                {
                    double baseline = Size.Height;
                    double baselineOffsetValue = Control.GetValue<double>(TextBlock.BaselineOffsetProperty);

                    if (!MathUtilities.IsZero(baselineOffsetValue))
                    {
                        baseline = baselineOffsetValue;
                    }

                    return -baseline;
                }
            }

            public override void Draw(DrawingContext drawingContext, Point origin)
            {             
                //noop            
            }
        }
    }
}
