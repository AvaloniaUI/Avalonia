// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 
// Description: Inline element. 
//

using Avalonia;
using Avalonia.Documents;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using MS.Internal;
using AvaloniaProperty = Avalonia.AvaloniaProperty;

namespace System.Windows.Documents 
{
    /// <summary>
    /// Inline element.
    /// </summary>
    [TextElementEditingBehaviorAttribute(IsMergeable = true, IsTypographicOnly = true)]
    public abstract class Inline : TextElement
    {
        //-------------------------------------------------------------------
        //
        // Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Public constructor.
        /// </summary>
        protected Inline() 
            : base()
        {
        }

        #endregion Constructors

        //-------------------------------------------------------------------
        //
        // Public Properties
        //
        //-------------------------------------------------------------------

        #region Public Properties

        /// <value>
        /// A collection of Inlines containing this one in its sequential tree.
        /// May return null if an element is not inserted into any tree.
        /// </value>
        public InlineCollection SiblingInlines
        {
            get
            {
                if (this.Parent == null)
                {
                    return null;
                }

                return new InlineCollection(this, /*isOwnerParent*/false);
            }
        }

        /// <summary>
        /// Returns an Inline immediately following this one
        /// on the same level of siblings
        /// </summary>
        public Inline NextInline
        {
            get
            {
                return this.NextElement as Inline;
            }
        }

        /// <summary>
        /// Returns an Inline immediately preceding this one
        /// on the same level of siblings
        /// </summary>
        public Inline PreviousInline
        {
            get
            {
                return this.PreviousElement as Inline;
            }
        }

        /// <summary>
        /// AvaloniaProperty for <see cref="BaselineAlignment" /> property.
        /// </summary>
        public static readonly StyledProperty<BaselineAlignment> BaselineAlignmentProperty = 
                AvaloniaProperty.Register<Inline, BaselineAlignment>(
                        "BaselineAlignment",
                                BaselineAlignment.Baseline);

        /// <summary>
        /// 
        /// </summary>
        public BaselineAlignment BaselineAlignment
        {
            get { return (BaselineAlignment) GetValue(BaselineAlignmentProperty); }
            set { SetValue(BaselineAlignmentProperty, value); }
        }

        /// <summary>
        /// AvaloniaProperty for <see cref="TextDecorations" /> property.
        /// </summary>
        public static readonly StyledProperty<TextDecorationCollection> TextDecorationsProperty = 
                AvaloniaProperty.Register<Inline, TextDecorationCollection>(
                        "TextDecorations");

        /// <summary>
        /// The TextDecorations property specifies decorations that are added to the text of an element.
        /// </summary>
        public TextDecorationCollection TextDecorations
        {
            get { return (TextDecorationCollection) GetValue(TextDecorationsProperty); }
            set { SetValue(TextDecorationsProperty, value); }
        }

        /// <summary>
        /// AvaloniaProperty for <see cref="FlowDirection" /> property.
        /// </summary>
        public static readonly StyledProperty<FlowDirection> FlowDirectionProperty =
            AvaloniaProperty.RegisterAttached<Inline, Inline, FlowDirection>(
                "FlowDirection",
                    FlowDirection.LeftToRight, // default value
                    true);

        /// <summary>
        /// The FlowDirection property specifies the flow direction of the element.
        /// </summary>
        public FlowDirection FlowDirection
        {
            get { return (FlowDirection)GetValue(FlowDirectionProperty); }
            set { SetValue(FlowDirectionProperty, value); }
        }

        #endregion Public Properties

        //-------------------------------------------------------------------
        //
        //  Internal Methods
        //
        //-------------------------------------------------------------------

        #region Internal Methods

        internal static Run CreateImplicitRun()
        {
            return new Run();
        }

        internal static InlineUIContainer CreateImplicitInlineUIContainer()
        {
            return new InlineUIContainer();
        }

        #endregion Internal Methods

        //-------------------------------------------------------------------
        //
        // Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods
        //private static bool IsValidBaselineAlignment(object o)
        //{
        //    BaselineAlignment value = (BaselineAlignment)o;
        //    return value == BaselineAlignment.Baseline
        //        || value == BaselineAlignment.Bottom
        //        || value == BaselineAlignment.Center
        //        || value == BaselineAlignment.Subscript
        //        || value == BaselineAlignment.Superscript
        //        || value == BaselineAlignment.TextBottom
        //        || value == BaselineAlignment.TextTop
        //        || value == BaselineAlignment.Top;
        //}

        #endregion Private Methods
    }
}
