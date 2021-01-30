// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: a high-level tool for editing text based FrameworkElements.
//

using MS.Internal;
using System.Collections.Generic;
//using System.Windows.Threading;
//using System.Globalization;
//using System.Xml;
using System.IO;
using System.Windows.Markup;
using Avalonia;

// Parser

#pragma warning disable 1634, 1691  // suppressing PreSharp warnings

namespace System.Windows.Documents
{
    /// <summary>
    /// TextRange is a high-level tool for editing text based FrameworkElements
    /// such as Text, TextFlow, or RichTextBox.
    /// </summary>
    public class TextRange : ITextRange
    {
        #region Constructors

        //------------------------------------------------------
        //
        // Constructors
        //
        //------------------------------------------------------

        /// <summary>
        /// Creates a new TextRange instance.
        /// </summary>
        /// <param name="position1">
        /// TextPointer specifying the static end of the new TextRange.
        /// </param>
        /// <param name="position2">
        /// TextPointer specifying the dynamic end of the new TextRange.
        /// </param>
        /// <exception cref="System.ArgumentException">
        /// Throws an ArgumentException if position1 and position2
        /// are not positioned within the same document, or if either
        /// TextPointer is null.
        /// </exception>
        /// <remarks>
        /// position1 represents the origin of the TextRange, as opposed
        /// to position2, which represents the UI active end of a TextRange.
        /// The distinction is important in applying selection heuristics to
        /// the exact placement of a new TextRange.
        /// 
        /// Note that the parameters to this method will not always match
        /// the Start and End properties of the new TextRange.  The exact
        /// positioning of a new TextRange is subject to hueristics specific
        /// to document types -- hueristics that match text selection behavior.
        /// </remarks>
        public TextRange(TextPointer position1, TextPointer position2) :
            this((ITextPointer)position1, (ITextPointer)position2)
        {
        }

        internal TextRange(ITextPointer position1, ITextPointer position2)
            : this(position1, position2, false /* ignoreTextUnitBoundaries */)
        {
        }

        /// <summary>
        /// Creates a new TextRange instance.
        /// </summary>
        /// <param name="position1">
        /// </param>
        /// TextPointer specifying the static end of the new TextRange.
        /// <param name="position2">
        /// TextPointer specifying the dynamic end of the new TextRange.
        /// </param>
        /// <param name="useRestrictiveXamlXmlReader">
        /// Boolean flag. False by default, set to true to disable external xaml loading in specific scenarios like StickyNotes annotation loading
        /// </param>
        internal TextRange(TextPointer position1, TextPointer position2, bool useRestrictiveXamlXmlReader) :
            this((ITextPointer)position1, (ITextPointer)position2)
        {
            _useRestrictiveXamlXmlReader = useRestrictiveXamlXmlReader;
        }

        // ignoreTextUnitBoundaries - true if normalization should ignore text
        // normalization (surrogates, combining marks, etc).
        // Used for fine-grained control by IMEs.
        internal TextRange(ITextPointer position1, ITextPointer position2, bool ignoreTextUnitBoundaries)
        {
            if (position1 == null)
            {
                throw new ArgumentNullException("position1");
            }
            if (position2 == null)
            {
                throw new ArgumentNullException("position2");
            }

            SetFlags(ignoreTextUnitBoundaries, Flags.IgnoreTextUnitBoundaries);

            ValidationHelper.VerifyPosition(position1.TextContainer, position1, "position1");
            ValidationHelper.VerifyPosition(position1.TextContainer, position2, "position2");

            TextRangeBase.Select(this, position1, position2);
        }

        #endregion Constructors   

        // *****************************************************
        // *****************************************************
        // *****************************************************
        //
        // Abstract TextRange Implementation
        //
        // *****************************************************
        // *****************************************************
        // *****************************************************

        //------------------------------------------------------
        //
        // ITextRange implementation
        //
        //------------------------------------------------------

        #region ITextRange Implementation

        //......................................................
        //
        // Selection Building
        //
        //......................................................

        /// <summary>
        /// <see cref="System.Windows.Documents.TextRange.Contains"/>
        /// </summary>
        bool ITextRange.Contains(ITextPointer position)
        {
            // ATTENTION: This implementation *must* be pure redirect to TextRangeBase. Otherwise TextSelection extensibility is broken
            // DO NOT ANY CODE IN THIS METHOD!
            return TextRangeBase.Contains(this, position);
        }

        /// <summary>
        /// <see cref="System.Windows.Documents.TextRange.Select"/>
        /// </summary>
        void ITextRange.Select(ITextPointer position1, ITextPointer position2)
        {
            // ATTENTION: This implementation *must* be pure redirect to TextRangeBase. Otherwise TextSelection extensibility is broken
            // DO NOT ANY CODE IN THIS METHOD!
            TextRangeBase.Select(this, position1, position2);
        }

        /// <summary>
        /// <see cref="System.Windows.Documents.TextRange.SelectWord"/>
        /// </summary>
        void ITextRange.SelectWord(ITextPointer position)
        {
            // ATTENTION: This implementation *must* be pure redirect to TextRangeBase. Otherwise TextSelection extensibility is broken
            // DO NOT ANY CODE IN THIS METHOD!
            TextRangeBase.SelectWord(this, position);
        }

        /// <summary>
        /// <see cref="System.Windows.Documents.TextRange.SelectParagraph"/>
        /// </summary>
        //void ITextRange.SelectParagraph(ITextPointer position)
        //{
        //    // ATTENTION: This implementation *must* be pure redirect to TextRangeBase. Otherwise TextSelection extensibility is broken
        //    // DO NOT ANY CODE IN THIS METHOD!
        //    TextRangeBase.SelectParagraph(this, position);
        //}

        /// <summary>
        /// <see cref="System.Windows.Documents.ITextRange.ApplyTypingHeuristics"/>
        /// </summary>
        void ITextRange.ApplyTypingHeuristics(bool overType)
        {
            // ATTENTION: This implementation *must* be pure redirect to TextRangeBase. Otherwise TextSelection extensibility is broken
            // DO NOT ANY CODE IN THIS METHOD!
            TextRangeBase.ApplyTypingHeuristics(this, overType);
        }

        object ITextRange.GetPropertyValue(AvaloniaProperty formattingProperty)
        {
            // ATTENTION: This implementation *must* be pure redirect to TextRangeBase. Otherwise TextSelection extensibility is broken
            // DO NOT ANY CODE IN THIS METHOD!
            return TextRangeBase.GetPropertyValue(this, formattingProperty);
        }

        StyledElement ITextRange.GetUIElementSelected()
        {
            // ATTENTION: This implementation *must* be pure redirect to TextRangeBase. Otherwise TextSelection extensibility is broken
            // DO NOT ANY CODE IN THIS METHOD!
            return TextRangeBase.GetUIElementSelected(this);
        }

        //......................................................
        //
        // Range Content Serialization
        //
        //......................................................

        //bool ITextRange.CanSave(string dataFormat)
        //{
        //    // ATTENTION: This implementation *must* be pure redirect to TextRangeBase. Otherwise TextSelection extensibility is broken
        //    // DO NOT ANY CODE IN THIS METHOD!
        //    return TextRangeBase.CanSave(this, dataFormat);
        //}

        //void ITextRange.Save(Stream stream, string dataFormat)
        //{
        //    // ATTENTION: This implementation *must* be pure redirect to TextRangeBase. Otherwise TextSelection extensibility is broken
        //    // DO NOT ANY CODE IN THIS METHOD!
        //    TextRangeBase.Save(this, stream, dataFormat, false);
        //}

        //void ITextRange.Save(Stream stream, string dataFormat, bool preserveTextElements)
        //{
        //    // ATTENTION: This implementation *must* be pure redirect to TextRangeBase. Otherwise TextSelection extensibility is broken
        //    // DO NOT ANY CODE IN THIS METHOD!
        //    TextRangeBase.Save(this, stream, dataFormat, preserveTextElements);
        //}

        //......................................................
        //
        // Change Notifications
        //
        //......................................................

        /// <summary>
        /// <see cref="ITextRange.BeginChange"/>
        /// </summary>
        void ITextRange.BeginChange()
        {
            // ATTENTION: This implementation *must* be pure redirect to TextRangeBase. Otherwise TextSelection extensibility is broken
            // DO NOT ANY CODE IN THIS METHOD!
            TextRangeBase.BeginChange(this);
        }

        /// <summary>
        /// <see cref="ITextRange.BeginChangeNoUndo"/>
        /// </summary>
        void ITextRange.BeginChangeNoUndo()
        {
            // ATTENTION: This implementation *must* be pure redirect to TextRangeBase. Otherwise TextSelection extensibility is broken
            // DO NOT ANY CODE IN THIS METHOD!
            TextRangeBase.BeginChangeNoUndo(this);
        }

        /// <summary>
        /// <see cref="ITextRange.EndChange()"/>
        /// </summary>
        void ITextRange.EndChange()
        {
            // ATTENTION: This implementation *must* be pure redirect to TextRangeBase. Otherwise TextSelection extensibility is broken
            // DO NOT ANY CODE IN THIS METHOD!
            TextRangeBase.EndChange(this, false /* disableScroll */, false /* skipEvents */);
        }

        /// <summary>
        /// <see cref="ITextRange.EndChange(bool,bool)"/>
        /// </summary>
        void ITextRange.EndChange(bool disableScroll, bool skipEvents)
        {
            // ATTENTION: This implementation *must* be pure redirect to TextRangeBase. Otherwise TextSelection extensibility is broken
            // DO NOT ANY CODE IN THIS METHOD!
            TextRangeBase.EndChange(this, disableScroll, skipEvents);
        }

        /// <summary>
        /// <see cref="ITextRange.DeclareChangeBlock()"/>
        /// </summary>
        IDisposable ITextRange.DeclareChangeBlock()
        {
            return new ChangeBlock(this, false /* disableScroll */);
        }

        /// <summary>
        /// <see cref="ITextRange.DeclareChangeBlock(bool)"/>
        /// </summary>
        IDisposable ITextRange.DeclareChangeBlock(bool disableScroll)
        {
            return new ChangeBlock(this, disableScroll);
        }

        /// <summary>
        /// <see cref="ITextRange.NotifyChanged"/>
        /// </summary>
        void ITextRange.NotifyChanged(bool disableScroll, bool skipEvents)
        {
            // ATTENTION: This implementation *must* be pure redirect to TextRangeBase. Otherwise TextSelection extensibility is broken
            // DO NOT ANY CODE IN THIS METHOD!
            TextRangeBase.NotifyChanged(this, disableScroll);
        }

        //------------------------------------------------------
        //
        //  ITextRange Properties
        //
        //------------------------------------------------------

        /// <summary>
        /// Set in the ctor.  If true, normalization will ignore text normalization
        /// (surrogates, combining marks, etc).
        /// Used for fine-grained control by IMEs.
        /// <see cref="ITextRange.Start"/>
        /// </summary>
        bool ITextRange.IgnoreTextUnitBoundaries
        {
            get
            {
                return CheckFlags(Flags.IgnoreTextUnitBoundaries);
            }
        }

        //......................................................
        //
        //  Boundary Positions
        //
        //......................................................

        /// <summary>
        /// <see cref="ITextRange.Start"/>
        /// </summary>
        ITextPointer ITextRange.Start
        {
            get
            {
                // ATTENTION: This implementation *must* be pure redirect to TextRangeBase. Otherwise TextSelection extensibility is broken
                // DO NOT ANY CODE IN THIS METHOD!
                return TextRangeBase.GetStart(this);
            }
        }

        /// <summary>
        /// <see cref="ITextRange.End"/>
        /// </summary>
        ITextPointer ITextRange.End
        {
            get
            {
                // ATTENTION: This implementation *must* be pure redirect to TextRangeBase. Otherwise TextSelection extensibility is broken
                // DO NOT ANY CODE IN THIS METHOD!
                return TextRangeBase.GetEnd(this);
            }
        }

        /// <summary>
        /// <see cref="ITextRange.IsEmpty"/>
        /// </summary>
        bool ITextRange.IsEmpty
        {
            get
            {
                // ATTENTION: This implementation *must* be pure redirect to TextRangeBase. Otherwise TextSelection extensibility is broken
                // DO NOT ANY CODE IN THIS METHOD!
                return TextRangeBase.GetIsEmpty(this);
            }
        }

        /// <summary>
        /// <see cref="ITextRange.TextSegments"/>
        /// </summary>
        List<TextSegment> ITextRange.TextSegments
        {
            get
            {
                // ATTENTION: This implementation *must* be pure redirect to TextRangeBase. Otherwise TextSelection extensibility is broken
                // DO NOT ANY CODE IN THIS METHOD!
                return TextRangeBase.GetTextSegments(this);
            }
        }

        //......................................................
        //
        //  Content - rich and plain
        //
        //......................................................

        /// <summary>
        /// <see cref="ITextRange.HasConcreteTextContainer"/>
        /// </summary>
        bool ITextRange.HasConcreteTextContainer
        {
            get
            {
                Invariant.Assert(_textSegments != null, "_textSegments must not be null");
                Invariant.Assert(_textSegments.Count > 0, "_textSegments.Count must be > 0");
                return _textSegments[0].Start is TextPointer;
            }
        }

        /// <summary>
        /// <see cref="ITextRange.Text"/>
        /// </summary>
        string ITextRange.Text
        {
            get
            {
                // ATTENTION: This implementation *must* be pure redirect to TextRangeBase. Otherwise TextSelection extensibility is broken
                // DO NOT ANY CODE IN THIS METHOD!
                return TextRangeBase.GetText(this);
            }

            set
            {
                // ATTENTION: This implementation *must* be pure redirect to TextRangeBase. Otherwise TextSelection extensibility is broken
                // DO NOT ANY CODE IN THIS METHOD!
                TextRangeBase.SetText(this, value);
            }
        }

        /// <summary>
        /// <see cref="ITextRange.Xml"/>
        /// </summary>
        //string ITextRange.Xml
        //{
        //    get
        //    {
        //        // ATTENTION: This implementation *must* be pure redirect to TextRangeBase. Otherwise TextSelection extensibility is broken
        //        // DO NOT ANY CODE IN THIS METHOD!
        //        return TextRangeBase.GetXml(this);
        //    }
        //}

        /// <summary>
        /// Ref count of open change blocks -- incremented/decremented
        /// around BeginChange/EndChange calls.
        /// <see cref="ITextRange.ChangeBlockLevel"/>
        /// </summary>
        int ITextRange.ChangeBlockLevel
        {
            get
            {
                // ATTENTION: This implementation *must* be pure redirect to TextRangeBase. Otherwise TextSelection extensibility is broken
                // DO NOT ANY CODE IN THIS METHOD!
                return TextRangeBase.GetChangeBlockLevel(this);
            }
        }

        //......................................................
        //
        //  Table Selection Properties
        //
        //......................................................

        /// <summary>
        /// <see cref="ITextRange.IsTableCellRange"/>
        /// </summary>
        //bool ITextRange.IsTableCellRange
        //{
        //    get
        //    {
        //        // ATTENTION: This implementation *must* be pure redirect to TextRangeBase. Otherwise TextSelection extensibility is broken
        //        // DO NOT ANY CODE IN THIS METHOD!
        //        return TextRangeBase.GetIsTableCellRange(this);
        //    }
        //}

        //------------------------------------------------------
        //
        //  ITextRange Events
        //
        //------------------------------------------------------

        /// <summary>
        /// <see cref="ITextRange.Changed"/>
        /// </summary>
        event EventHandler ITextRange.Changed
        {
            add
            {
                this.Changed += value;
            }

            remove
            {
                this.Changed -= value;
            }
        }

        /// <summary>
        /// <see cref="ITextRange.FireChanged"/>
        /// </summary>
        void ITextRange.FireChanged()
        {
            if (this.Changed != null)
            {
                this.Changed(this, EventArgs.Empty);
            }
        }

        //------------------------------------------------------
        //
        //  ITextRange Private Fields Accessors
        //
        //------------------------------------------------------

        /// <summary>
        /// Defines a state of this text range
        /// <see cref="ITextRange._IsTableCellRange"/>
        /// </summary>
        //bool ITextRange._IsTableCellRange
        //{
        //    get
        //    {
        //        return CheckFlags(Flags.IsTableCellRange);
        //    }
        //    set
        //    {
        //        SetFlags(value, Flags.IsTableCellRange);
        //    }
        //}

        /// <summary>
        /// A collection of TextSegments. Contains at least one segment.
        /// <see cref="ITextRange._TextSegments"/>
        /// </summary>
        List<TextSegment> ITextRange._TextSegments
        {
            get
            {
                return _textSegments;
            }
            set
            {
                _textSegments = value;
            }
        }

        /// <summary>
        /// Count of nested move sequences.
        /// <see cref="ITextRange._ChangeBlockLevel"/>
        /// </summary>
        int ITextRange._ChangeBlockLevel
        {
            get
            {
                return _changeBlockLevel;
            }

            set
            {
                _changeBlockLevel = value;
            }
        }

        /// <summary>
        /// <see cref="ITextRange._ChangeBlockUndoRecord"/>
        /// </summary>
        ChangeBlockUndoRecord ITextRange._ChangeBlockUndoRecord
        {
            get
            {
                return _changeBlockUndoRecord;
            }

            set
            {
                _changeBlockUndoRecord = value;
            }
        }

        /// <summary>
        /// Set true if a Changed event is pending.
        /// <see cref="ITextRange._IsChanged"/>
        /// </summary>
        bool ITextRange._IsChanged
        {
            get
            {
                return _IsChanged;
            }

            set
            {
                _IsChanged = value;
            }
        }

        /// <summary>
        /// ContentGeneration counter storage implementation
        /// <see cref="ITextRange._ContentGeneration"/>
        /// </summary>
        uint ITextRange._ContentGeneration
        {
            get
            {
                return _ContentGeneration;
            }
            set
            {
                _ContentGeneration = value;
            }
        }

        #endregion ITextRange Implementation


        // *****************************************************
        // *****************************************************
        // *****************************************************
        //
        // Concrete TextRange Implementation
        //
        // *****************************************************
        // *****************************************************
        // *****************************************************

        //------------------------------------------------------
        //
        // Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        //......................................................
        //
        // Selection Building
        //
        //......................................................

        /// <summary>
        /// Determines if a TextPointer is within this TextRange.
        /// </summary>
        /// <param name="textPointer">
        /// The TextPointer to test.
        /// </param>
        /// <exception cref="System.ArgumentException">
        /// Throws an ArgumentException if textPointer
        /// are not positioned within the same document.
        /// </exception>
        /// <returns>
        /// Returns true if textPosition is contained within this TextRange, false
        /// otherwise.
        /// </returns>
        /// <remarks>
        /// Note TextRanges may be disjoint (for instance, when a column in a
        /// Table is selected), so calling this method is not always equivalent
        /// to testing for inclusion against the raw Start and End properties.
        /// 
        /// If textPointer is located at the TextRange Start or End position,
        /// it is contained by the TextRange.
        /// </remarks>
        // This is behavior we want to eventually support:
        //
        // If textPointer is exactly at the edge of this TextRange (ie,
        // textPointer == this.Start or textPointer == this.End),
        // textPointer.LogicalDirection is used to determine whether or not
        // the TextPointer is contained.  If the LogicalDirection points to
        // content inside the TextRange, this method returns true.
        // Because of this rule, an empty TextRange will never contain
        // any TextPointer.
        public bool Contains(TextPointer textPointer)
        {
            return ((ITextRange)this).Contains(textPointer);
        }

        /// <summary>
        /// Repositions this TextRange to cover specified content.
        /// </summary>
        /// <param name="position1">
        /// TextPointer specifying the static end of the new TextRange.
        /// </param>
        /// <param name="position2">
        /// TextPointer specifying the dynamic end of the new TextRange.
        /// </param>
        /// <exception cref="System.ArgumentException">
        /// Throws an ArgumentException if position1 and position2
        /// are not positioned within the same document, or if either
        /// TextPointer is null.
        /// </exception>
        /// <remarks>
        /// position1 represents the origin of the TextRange, as opposed
        /// to position2, which represents the UI active end of a TextRange.
        /// The distinction is important in applying selection heuristics to
        /// the exact placement of the TextRange.
        /// 
        /// Note that the parameters to this method will not always match
        /// the Start and End properties of the repositioned TextRange.  The
        /// exact positioning of a TextRange is subject to hueristics
        /// specific to document types -- hueristics that match text selection
        /// behavior.
        /// </remarks>
        public void Select(TextPointer position1, TextPointer position2)
        {
            ((ITextRange)this).Select(position1, position2);
        }

        /// <summary>
        /// Selects the word containing a TextPointer.
        /// </summary>
        /// <param name="textPointer">
        /// A TextPointer containing a word to select.
        /// </param>
        /// <exception cref="System.ArgumentException">
        /// Throws an ArgumentException if textPointer is not positioned within the
        /// same document.
        /// </exception>
        internal void SelectWord(TextPointer textPointer)
        {
            ((ITextRange)this).SelectWord(textPointer);
        }

        /// <summary>
        /// Selects a paragraph around the given position.
        /// </summary>
        /// <param name="position">
        /// A position identifying a paragraph to select.
        /// </param>
        //internal void SelectParagraph(ITextPointer position)
        //{
        //    ((ITextRange)this).SelectParagraph(position);
        //}

        //......................................................
        //
        // Plain Text Modification
        //
        //......................................................

        //......................................................
        //
        // Rich Text Formatting
        //
        //......................................................

        /// <summary>
        /// Applies a formatting property to this TextRange.
        /// </summary>
        /// <param name="formattingProperty">
        /// Property to apply.
        /// </param>
        /// <param name="value">
        /// Specifies a value for the property.
        /// </param>
        /// <remarks>
        /// This method applies the specificed property directly to the
        /// TextRange's content through the application of Inline elements.
        /// </remarks>
        public void ApplyPropertyValue(AvaloniaProperty formattingProperty, object value)
        {
            this.ApplyPropertyValue(formattingProperty, value, /*applyToParagraphs*/false, PropertyValueAction.SetValue);
        }

        /// <summary>
        /// Applies a formatting property to this TextRange.
        /// </summary>
        /// <param name="formattingProperty">
        /// Property to apply.
        /// </param>
        /// <param name="value">
        /// Specifies a value for the property.
        /// </param>
        /// <param name="applyToParagraphs">
        /// This parameter is used to resolve the ambiguity for overlapping inherited properties 
        /// that apply to both inline and paragraph elements.
        /// </param>
        /// <remarks>
        /// This method applies the specificed property directly to the
        /// TextRange's content through the application of Inline elements.
        /// </remarks>
        internal void ApplyPropertyValue(AvaloniaProperty formattingProperty, object value, bool applyToParagraphs)
        {
            this.ApplyPropertyValue(formattingProperty, value, applyToParagraphs, PropertyValueAction.SetValue);
        }

        /// <summary>
        /// Applies a formatting property to this TextRange.
        /// </summary>
        /// <param name="formattingProperty">
        /// Property to apply.
        /// </param>
        /// <param name="value">
        /// Specifies a value for the property.
        /// </param>
        /// <param name="applyToParagraphs">
        /// This parameter is used to resolve the ambiguity for overlapping inherited properties 
        /// that apply to both inline and paragraph elements.
        /// </param>
        /// <param name="propertyValueAction">
        /// Specifies how to apply the given value - use it for setting,
        /// for increasing or for decreasing existing values.
        /// This parameter must have PropertyValueAction.SetValue for all properties that
        /// cannot be incremented or decremented by their type.
        /// </param>
        internal void ApplyPropertyValue(AvaloniaProperty formattingProperty, object value, bool applyToParagraphs, PropertyValueAction propertyValueAction)
        {
            Invariant.Assert(this.HasConcreteTextContainer, "Can't apply property to non-TextContainer range!");

            if (formattingProperty == null)
            {
                throw new ArgumentNullException("formattingProperty");
            }

            if (!TextSchema.IsCharacterProperty(formattingProperty) /*&&*/
               /* !TextSchema.IsParagraphProperty(formattingProperty)*/)
            {
                #pragma warning suppress 6506 // formattingProperty is obviously not null
                throw new ArgumentException(/*SR.Get(SRID.TextEditorPropertyIsNotApplicableForTextFormatting, formattingProperty.Name)*/);
            }

            // Convert property value from a string to object if needed
            if ((value is string) && formattingProperty.PropertyType != typeof(string))
            {
                System.ComponentModel.TypeConverter typeConverter = System.ComponentModel.TypeDescriptor.GetConverter(formattingProperty.PropertyType);
                Invariant.Assert(typeConverter != null);
                value = typeConverter.ConvertFromString((string)value);
            }

            // Check if the value is appropriate for the property
            if (!formattingProperty.IsValidValue(value) &&
                !(formattingProperty.PropertyType == typeof(Thickness) && (value is Thickness)))
            {
                // We exclude checking thcickness values because we have special treatment for negative values
                // in TextRangeEdit.SetParagraphProperty - negative values mean: "leave the value as is".
                throw new ArgumentException(/*SR.Get(SRID.TextEditorTypeOfParameterIsNotAppropriateForFormattingProperty, value == null ? "null" : value.GetType().Name, formattingProperty.Name), "value"*/);
            }

            // Check propertyValueAction validity
            if (propertyValueAction != PropertyValueAction.SetValue &&
                propertyValueAction != PropertyValueAction.IncreaseByAbsoluteValue &&
                propertyValueAction != PropertyValueAction.DecreaseByAbsoluteValue &&
                propertyValueAction != PropertyValueAction.IncreaseByPercentageValue &&
                propertyValueAction != PropertyValueAction.DecreaseByPercentageValue)
            {
                throw new ArgumentException(/*SR.Get(SRID.TextRange_InvalidParameterValue), "propertyValueAction"*/);
            }
            // Check if propertyValueAction is applicable to this property
            if (propertyValueAction != PropertyValueAction.SetValue &&
                !TextSchema.IsPropertyIncremental(formattingProperty))
            {
                throw new ArgumentException(/*SR.Get(SRID.TextRange_PropertyCannotBeIncrementedOrDecremented, formattingProperty.Name), "propertyValueAction"*/);
            }

            ApplyPropertyToTextVirtual(formattingProperty, value, applyToParagraphs, propertyValueAction);
        }

        /// <summary>
        /// Removes all Inline formatting properties from this range.
        /// Affects only Inline elements: splits the on range borders
        /// and deletes all Inlines inside the range.
        /// Properties set on Paragraphs and other enclosing Block elements
        /// remain intact.
        /// </summary>
        public void ClearAllProperties()
        {
            Invariant.Assert(this.HasConcreteTextContainer, "Can't clear properties in non-TextContainer range");

            ClearAllPropertiesVirtual();
        }

        /// <summary>
        /// Gets the value of the given formatting property on this range.
        /// </summary>
        /// <param name="formattingProperty">
        /// Property value to get.
        /// </param>
        /// <returns>
        /// Value of the requested property.
        /// </returns>
        public object GetPropertyValue(AvaloniaProperty formattingProperty)
        {
            if (formattingProperty == null)
            {
                throw new ArgumentNullException("formattingProperty");
            }
            if (!TextSchema.IsCharacterProperty(formattingProperty) /*&&*/
                /*!TextSchema.IsParagraphProperty(formattingProperty)*/)
            {
                #pragma warning suppress 6506 // formattingProperty is obviously not null
                throw new ArgumentException(/*SR.Get(SRID.TextEditorPropertyIsNotApplicableForTextFormatting, formattingProperty.Name)*/);
            }

            // Redirect to virtual implementation - to allow extensiblity on TextSelection level
            return ((ITextRange)this).GetPropertyValue(formattingProperty);
        }

        /// <summary>
        /// Returns a UIElement if it is selected by this range as its
        /// only content. If there is no UIElement in the range or
        /// if there is any other printable content (charaters, other
        /// UIElements, structural boundaries crossed), the method returns
        /// null.
        /// </summary>
        /// <returns></returns>
        internal StyledElement GetUIElementSelected()
        {
            return ((ITextRange)this).GetUIElementSelected();
        }

        //......................................................
        //
        //  Data Conversion Support
        //
        //......................................................

        /// <summary>
        /// Detects whether the content of a range can be converted
        /// to a requested format.
        /// </summary>
        /// <param name="dataFormat">
        /// A string indicatinng a requested format.
        /// </param>
        /// <returns>
        /// True if the given format is supported; false otherwise.
        /// </returns>
        //public bool CanSave(string dataFormat)
        //{
        //    return ((ITextRange)this).CanSave(dataFormat);
        //}


        /// <summary>
        /// Detects whether the content of a range can be converted
        /// from a requested format.
        /// </summary>
        /// <param name="dataFormat">
        /// A string indicatinng a requested format.
        /// </param>
        /// <returns>
        /// True if the given format is supported; false otherwise.
        /// </returns>
        //public bool CanLoad(string dataFormat)
        //{
        //    return TextRangeBase.CanLoad(this, dataFormat);
        //}

        /// <summary>
        /// Writes the contents of the range into a stream
        /// in a requested format.
        /// </summary>
        /// <param name="stream">
        /// Writeable Stream - a destination for the serialized content.
        /// Must be empty on entry. Will contain a data converted from the range
        /// in a requested format.
        /// After saving the stream remains opened.
        /// The stream position after the saving operation
        /// is undefined.
        /// </param>
        /// <param name="dataFormat">
        /// A string denoting one of supported data conversions:
        /// DataFormats.Text, DataFormats.Xaml, DataFormats.XamlPackage,
        /// DataFormats.Rtf.
        /// </param>
        /// <remarks>
        /// When dataFormat requested is not supported
        /// the method will throw an exception.
        /// To detect whether the given format is supported
        /// call CanSave method.
        /// </remarks>
        //public void Save(Stream stream, string dataFormat)
        //{
        //    ((ITextRange)this).Save(stream, dataFormat);
        //}

        /// <summary>
        /// Writes the contents of the range into a stream
        /// in a requested format.
        /// </summary>
        /// <param name="stream">
        /// Writeable Stream - a destination for the serialized content.
        /// Must be empty on entry. Will contain a data converted from the range
        /// in a requested format.
        /// After saving the stream remains opened.
        /// The stream position after the saving operation
        /// is undefined.
        /// </param>
        /// <param name="dataFormat">
        /// A string denoting one of supported data conversions:
        /// DataFormats.Text, DataFormats.Xaml, DataFormats.XamlPackage,
        /// DataFormats.Rtf.
        /// </param>
        /// <param name="preserveTextElements">
        /// If TRUE, TextElements are saved as-is.  If FALSE, they are upcast
        /// to their base type.  Non-complex custom properties are also saved if this parameter
        /// is true.  This parameter is only used for DataFormats.Xaml and
        /// DataFormats.XamlPackage and is ignored by other formats.
        /// </param>
        /// <remarks>
        /// When dataFormat requested is not supported
        /// the method will throw an exception.
        /// To detect whether the given format is supported
        /// call CanSave method.
        /// </remarks>
        //public void Save(Stream stream, string dataFormat, bool preserveTextElements)
        //{
        //    ((ITextRange)this).Save(stream, dataFormat, preserveTextElements);
        //}

        /// <summary>
        /// Reads the contents of the range from the stream
        /// in a requested format.
        /// </summary>
        /// <param name="stream">
        /// Readable Stream - a source for the serialized content.
        /// Expected to contain data in the specified dataFormat.
        /// The content will be read from the beginning of the stream
        /// if the stream is Seekable (CanSeek=true),
        /// otherwize the content will be read from the current
        /// position of the stream.
        /// After loading the stream remains opened.
        /// The stream position after the loading operation
        /// is undefined.
        /// </param>
        /// <param name="dataFormat">
        /// A string denoting one of supported data conversions:
        /// DataFormats.Text, DataFormats.Xaml, DataFormats.XamlPackage,
        /// DataFormats.Rtf
        /// </param>
        /// <remarks>
        /// When dataFormat requested is not supported
        /// the method will throw an exception.
        /// To detect whether the given format is supported
        /// call CanLoad method.
        /// </remarks>
        //public void Load(Stream stream, string dataFormat)
        //{
        //    LoadVirtual(stream, dataFormat);
        //}

        //......................................................
        //
        //  Image support
        //
        //......................................................

        // Implements smart insertion logic for embedded elements:
        // when inserted into an empty paragraph uses BlockUIContainer wrapper,
        // otherwise uses InlineUIContainer wrapper.
        //internal void InsertEmbeddedUIElement(StyledElement embeddedElement)
        //{
        //    Invariant.Assert(embeddedElement != null);

        //    this.InsertEmbeddedUIElementVirtual(embeddedElement);
        //}

        // Inserts an image maintaining its size within a hard-coded predefined limit,
        // and keeping its aspect ratio. Uses a smart insertion logic - same
        // as in InsertEmbeddedUIElement method.
        // This method is called from Lexicon to insert the image.
        //internal void InsertImage(System.Windows.Controls.Image image)
        //{
        //    // The following code may change some of the properties of passed Image. 
        //    // So, we should really assert here that the image element is not parented by another tree
        //    // and all caller code should obey that contract.
        //    // However, for the time being this is an internal helper and only copy/paste code invokes this.
        //    // Note that this same comment applies to InsertEmbeddedUIElement() as well.

        //    System.Windows.Media.Imaging.BitmapSource bitmapSource = (System.Windows.Media.Imaging.BitmapSource)image.Source;

        //    Invariant.Assert(bitmapSource != null);

        //    const double MaxImageHeight = 300.0;

        //    if (double.IsNaN(image.Height))
        //    {
        //        // Define image dimenstions maintaining its native aspect ratio,
        //        // but not bigger than a predefined maximum.
        //        if (bitmapSource.PixelHeight < MaxImageHeight)
        //        {
        //            image.Height = bitmapSource.PixelHeight;
        //        }
        //        else
        //        {
        //            image.Height = MaxImageHeight;
        //        }
        //    }

        //    if (double.IsNaN(image.Width))
        //    {
        //        // Define image dimenstions maintaining its native aspect ratio,
        //        // but not bigger than a predefined maximum.
        //        if (bitmapSource.PixelHeight < MaxImageHeight)
        //        {
        //            image.Width = bitmapSource.PixelWidth;
        //        }
        //        else
        //        {
        //            image.Width = (MaxImageHeight / bitmapSource.PixelHeight) * bitmapSource.PixelWidth;
        //        }
        //    }

        //    this.InsertEmbeddedUIElement(image);
        //}

        //......................................................
        //
        //  Range Serialization
        //
        //......................................................

        // Worker for Xml property setter; enables extensibility for TextSelection
        //internal virtual void SetXmlVirtual(TextElement fragment)
        //{
        //    if (!this.IsTableCellRange)
        //    {
        //        TextRangeSerialization.PasteXml(this, fragment);
        //    }
        //}

        // Worker for Load public method; enables extensibility for TextSelection
        //internal virtual void LoadVirtual(Stream stream, string dataFormat)
        //{
        //    TextRangeBase.Load(this, stream, dataFormat);
        //}

        //......................................................
        //
        //  Table Editing
        //
        //......................................................

        /// <summary>
        /// Inserts a table with a given number of rows and columns.
        /// </summary>
        /// <param name="rowCount">
        /// A number of rows generated in a table.
        /// </param>
        /// <param name="columnCount">
        /// A number of columns generated in each row of a table
        /// </param>
        /// <returns>
        /// Table element innserted.
        /// </returns>
        //internal Table InsertTable(int rowCount, int columnCount)
        //{
        //    Invariant.Assert(this.HasConcreteTextContainer, "InsertTable: TextRange must belong to non-abstract TextContainer");

        //    return InsertTableVirtual(rowCount, columnCount);
        //}

        /// <summary>
        /// Inserts several table rows before or after the selection depending on
        /// sign of rowCount parameter.
        /// </summary>
        /// <param name="rowCount">
        /// Absolute value of a parameter specifies number of rows inserted,
        /// the sign indicates before (negative) or after (positive) the range
        /// new rows must be inserted.
        /// </param>
        /// <returns>
        /// TextRange spanning all insereted rows.
        /// </returns>
        /// <remarks>
        /// Candidate for public method - when Table editing exposed
        /// </remarks>
        //internal TextRange InsertRows(int rowCount)
        //{
        //    Invariant.Assert(this.HasConcreteTextContainer, "InsertRows: TextRange must belong to non-abstract TextContainer");

        //    return InsertRowsVirtual(rowCount);
        //}

        /// <summary>
        /// Deletes rows identified by this range.
        /// </summary>
        /// <returns>
        /// True if row range was selected and successfully deleted.
        /// False if a source range did not contain rows; no actions done in this case.
        /// </returns>
        /// <remarks>
        /// Candidate for public method - when Table editing exposed
        /// </remarks>
        //internal bool DeleteRows()
        //{
        //    Invariant.Assert(this.HasConcreteTextContainer, "DeleteRows: TextRange must belong to non-abstract TextContainer");

        //    return DeleteRowsVirtual();
        //}

        /// <summary>
        /// Inserts several table columns before or after the selection depending on
        /// sign of rowCount parameter.
        /// </summary>
        /// <param name="columnCount">
        /// Absolute value of a parameter specifies number of columns inserted,
        /// the sign indicates before (negative) or after (positive) the range
        /// new columns must be inserted.
        /// </param>
        /// <returns>
        /// TextRange spanning all insereted columns.
        /// </returns>
        /// <remarks>
        /// Candidate for public method - when Table editing exposed
        /// </remarks>
        //internal TextRange InsertColumns(int columnCount)
        //{
        //    Invariant.Assert(this.HasConcreteTextContainer, "InsertColumns: TextRange must belong to non-abstract TextContainer");

        //    return InsertColumnsVirtual(columnCount);
        //}

        /// <summary>
        /// Deletes columns identified by this range.
        /// </summary>
        /// <returns>
        /// True if cell range was selected and columns were successfully deleted.
        /// False if a source range did not contain cells; no actions done in this case.
        /// </returns>
        /// <remarks>
        /// Candidate for public method - when Table editing exposed
        /// </remarks>
        //internal bool DeleteColumns()
        //{
        //    Invariant.Assert(this.HasConcreteTextContainer, "DeleteColumns: TextRange must belong to non-abstract TextContainer");

        //    return DeleteColumnsVirtual();
        //}

        /// <summary>
        /// Merges all cells in a given range into one cell.
        /// </summary>
        /// <returns>
        /// TextRange containing the resulting merged cell.
        /// </returns>
        /// <remarks>
        /// Candidate for public method - when Table editing exposed
        /// </remarks>
        //internal TextRange MergeCells()
        //{
        //    Invariant.Assert(this.HasConcreteTextContainer, "MergeCells: TextRange must belong to non-abstract TextContainer");

        //    return MergeCellsVirtual();
        //}

        /// <summary>
        /// Splits a merged cell in vertical and in horizontal directions
        /// </summary>
        /// <param name="splitCountHorizontal">
        /// Number of cells created to the right of the current cell.
        /// Must be less than current cell's ColumnSpan property value.
        /// </param>
        /// <param name="splitCountVertical">
        /// Number of cells created below the current cell.
        /// Must be less than current cell's RowSpan property value.
        /// </param>
        /// <returns>
        /// Table range spanning initial cell and all split cells to the left and below it.
        /// </returns>
        /// <remarks>
        /// Candidate for public method - when Table editing exposed
        /// </remarks>
        //internal TextRange SplitCell(int splitCountHorizontal, int splitCountVertical)
        //{
        //    Invariant.Assert(this.HasConcreteTextContainer, "SplitCells: TextRange must belong to non-abstract TextContainer");

        //    return SplitCellVirtual(splitCountHorizontal, splitCountVertical);
        //}

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        //......................................................
        //
        //  Boundary Positions
        //
        //......................................................

        /// <summary>
        /// TextPointer preceding all content.
        /// </summary>
        /// <remarks>
        /// The TextPointer returned always has its IsFrozen property set true.
        /// </remarks>
        public TextPointer Start
        { 
            get
            { 
                return (TextPointer)((ITextRange)this).Start;
            }
        }

        /// <summary>
        /// TextPointer following all content.
        /// </summary>
        /// <remarks>
        /// The TextPointer returned always has its IsFrozen property set true.
        /// </remarks>
        public TextPointer End
        {
            get
            {
                return (TextPointer)((ITextRange)this).End;
            }
        }

        /// <summary>
        ///  Returns true if this TextRange spans no content.
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                return ((ITextRange)this).IsEmpty;
            }
        }

        //......................................................
        //
        //  Content - rich and plain
        //
        //......................................................

        internal bool HasConcreteTextContainer
        {
            get
            {
                return ((ITextRange)this).HasConcreteTextContainer;
            }
        }

        internal StyledElement ContainingFrameworkElement
        {
            get
            {
                if (this.HasConcreteTextContainer)
                {
                    return ((TextPointer)this.Start).ContainingFrameworkElement;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        ///  Get and set the text spanned by this text range.
        ///  New line characters and paragraph breaks are
        ///  considered as equivalent from plain text perspective,
        ///  so all kinds of breaks are converted into new lines
        ///  on get, and converted into paragraph breaks
        ///  on set (if back-end store allows that, or
        ///  remain new line characters otherwise).
        /// </summary>
        /// <remarks>
        ///  The selected content is collapsed before setting text.
        ///  Collapse assumes mering all block elements crossed by
        ///  this range - from the two neighboring block the preceding
        ///  one survives.
        ///  Character formatting elements are not merged.
        ///  They are eliminated only if they become empty.
        /// </remarks>
        public string Text
        {
            get
            {
                return ((ITextRange)this).Text;
            }

            set
            {
                ((ITextRange)this).Text = value;
            }
        }

        /// <summary>
        /// Returns the serialized content of this TextRange, in xml format.
        /// </summary>
        //internal string Xml
        //{
        //    get
        //    {
        //        return ((ITextRange)this).Xml;
        //    }

        //    set
        //    {
        //        // Note that setter for this property is not in ITextRange
        //        // so we use virtual mechanism for extensibility in TextSelection
        //        TextRangeBase.BeginChange(this);
        //        try
        //        {
        //            // Parse the fragment into a separate subtree
        //            object xamlObject = XamlReader.Load(new XmlTextReader(new System.IO.StringReader(value)), _useRestrictiveXamlXmlReader);
        //            TextElement fragment = xamlObject as TextElement;

        //            if (fragment != null)
        //            {
        //                this.SetXmlVirtual(fragment);
        //            }
        //        }
        //        finally
        //        {
        //            TextRangeBase.EndChange(this);
        //        }
        //    }
        //}

        //......................................................
        //
        //  Table Selection Properties
        //
        //......................................................

        //internal bool IsTableCellRange
        //{
        //    get
        //    {
        //        return ((ITextRange)this).IsTableCellRange;
        //    }
        //}

        #endregion Public Properties

        #region Public Events

        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------

        /// <summary>
        /// The Changed event is fired when the range is repositioned
        /// to cover a new span of text.
        /// 
        /// The EventHandler delegate is called with this TextRange
        /// as the sender, and EventArgs.Empty as the argument.
        /// </summary>
        public event EventHandler Changed;

        #endregion Public Events

        #region Internal methods

        //......................................................
        //
        // Change Notifications
        //
        //......................................................

        /// <summary>
        /// Begins a change block.
        /// </summary>
        /// <remarks>
        /// This method, along with EndChange or DeclareChangeBlock, provides
        /// an optional means of controlling the timing of events fired off this
        /// TextRange and its underlying document.
        /// 
        /// Events are fired whenever a TextRange is moved, or its content
        /// modified.  Normally, this happens immediately, before the instigating
        /// method (Select, set_Text, etc.) returns.  This can cause trouble if
        /// all event listeners are not coordinated -- in particular because
        /// reentrant edits are possible a caller has no guarantees about the
        /// state of the TextRange or document after making any state changes.
        /// 
        /// Calling BeginChange declares a "change block", a scope during which
        /// all state changes are recorded but no events are raised.  Only when
        /// a matching EndChange call is made will events be raised.
        /// 
        /// The pattern becomes:
        /// 
        /// range.BeginChange();
        /// try // Use a try/finally to ensure the EndChange is always called.
        /// {
        ///     .. // Reposition the range, or modify the content.
        /// }
        /// finally
        /// {
        ///     range.EndChange(); // events are raised.
        /// }
        /// 
        /// Callers must take care to always match every call to BeginChange
        /// with a matching EndChange, or else the TextRange will stop raising
        /// events.  The DeclareChangeBlock method provides a handy usage pattern
        /// for C# developers taking advantage of the "using" statement.
        /// 
        /// Begin/EndChange calls are reference counted and may be nested.  Only
        /// when the outermost EndChange call is made will events be raised.
        /// </remarks>
        internal void BeginChange()
        {
            ((ITextRange)this).BeginChange();
        }

        /// <summary>
        /// Closes a change block.
        /// </summary>
        /// <remarks>
        /// <see cref="TextRange.BeginChange"/>
        /// Each call the BeginChange must be followed by a call to this method,
        /// which raises public events for any editing operations performed
        /// within the block.
        /// </remarks>
        internal void EndChange()
        {
            ((ITextRange)this).EndChange();
        }

        /// <summary>
        /// Begins a change block.
        /// </summary>
        /// <remarks>
        /// This method is an alternative to the BeginChange/EndChange usage
        /// pattern.
        /// 
        /// Calling this method is equivalent to calling the BeginChange method,
        /// except additionally an IDisposable is returned.  Disposing the
        /// object is equivalent to calling EndChange.
        /// 
        /// This method is intended for C# users taking advantage of the "using"
        /// statement.  Instead of writing
        /// 
        /// range.BeginChange();
        /// try // Use a try/finally to ensure the EndChange is always called.
        /// {
        ///     .. // Reposition the range, or modify the content.
        /// }
        /// finally
        /// {
        ///     range.EndChange(); // events are raised.
        /// }
        /// 
        /// a more concise (and exactly equivalent)
        /// 
        /// using (new range.DeclareChangeBlock())
        /// {
        ///     .. // Reposition the range, or modify the content.
        /// } // Events are raised the Dispose takes place.
        /// 
        /// is possible.
        /// </remarks>
        internal IDisposable DeclareChangeBlock()
        {
            return ((ITextRange)this).DeclareChangeBlock();
        }

        /// <summary>
        /// Begins a change block.
        /// </summary>
        /// <remarks>
        /// When disableScroll == true, the caret will not automatically scroll into view.
        /// </remarks>
        internal IDisposable DeclareChangeBlock(bool disableScroll)
        {
            return ((ITextRange)this).DeclareChangeBlock(disableScroll);
        }

        // Set true if a Changed event is pending.
        // This method only intended for use by derived classes
        // (but may not be declared "protected" without public
        // exposure).
        internal bool _IsChanged
        {
            get
            {
                return CheckFlags(Flags.IsChanged);
            }

            set
            {
                SetFlags(value, Flags.IsChanged);
            }
        }

        #endregion Internal methods

        //------------------------------------------------------
        //
        // Internal Virtual Methods - TextSelection Extensibility
        //
        //------------------------------------------------------

        #region Internal Virtual Methods

        //......................................................
        //
        //  Formatting
        //
        //......................................................

        // Worker for AppendEmbeddedElement; enabled extensibility for TextSelection
        //internal virtual void InsertEmbeddedUIElementVirtual(StyledElement embeddedElement)
        //{
        //    Invariant.Assert(this.HasConcreteTextContainer, "Can't insert embedded object to non-TextContainer range!");
        //    Invariant.Assert(embeddedElement != null);

        //    TextRangeBase.BeginChange(this);
        //    try
        //    {
        //        // Delete existing selected content
        //        this.Text = String.Empty;

        //        // Calling EnsureInsertionPosition has the effect of inserting a paragraph 
        //        // before insert the embedded UIElement at BlockUIContainer or InlineUIConatiner.
        //        TextPointer startPosition = TextRangeEditTables.EnsureInsertionPosition(this.Start);

        //        // Choose what wrapper to use - BlockUIContainer or InlineUIContainer -
        //        // depending on the current paragraph emptiness
        //        Paragraph paragraph = startPosition.Paragraph;

        //        if (paragraph != null)
        //        {
        //            if (Paragraph.HasNoTextContent(paragraph))
        //            {
        //                // Use BlockUIContainer as a replacement of the current paragraph
        //                BlockUIContainer blockUIContainer = new BlockUIContainer(embeddedElement);

        //                // Translate embedded element's horizontal alignment property to the BlockUIContainer's text alignment
        //                blockUIContainer.TextAlignment = TextRangeEdit.GetTextAlignmentFromHorizontalAlignment(embeddedElement.HorizontalAlignment);

        //                // Replace paragraph with BlockUIContainer
        //                paragraph.SiblingBlocks.InsertAfter(paragraph, blockUIContainer);
        //                paragraph.SiblingBlocks.Remove(paragraph);
        //                this.Select(blockUIContainer.ContentStart, blockUIContainer.ContentEnd);
        //            }
        //            else
        //            {
        //                // Use InlineUIContainer
        //                InlineUIContainer inlineUIContainer = new InlineUIContainer(embeddedElement);
        //                TextPointer insertionPosition = TextRangeEdit.SplitFormattingElements(this.Start, /*keepEmptyFormatting:*/false);
        //                insertionPosition.InsertTextElement(inlineUIContainer);
        //                this.Select(inlineUIContainer.ElementStart, inlineUIContainer.ElementEnd);
        //            }
        //        }
        //    }
        //    finally
        //    {
        //        TextRangeBase.EndChange(this);
        //    }
        //}

        // Worker for ApplyProperty; enables extensibility for TextSelection
        internal virtual void ApplyPropertyToTextVirtual(AvaloniaProperty formattingProperty, object value, bool applyToParagraphs, PropertyValueAction propertyValueAction)
        {
            TextRangeBase.BeginChange(this);
            try
            {
                for (int i = 0; i < _textSegments.Count; i++)
                {
                    TextSegment textSegment = _textSegments[i];

                    if (formattingProperty == Inline.FlowDirectionProperty)
                    {
                        // FlowDirection is an overlapping inheritable property that needs special handling. 
                        // We apply it as a paragraph property when:
                        //  1. applyToParagraphs = true or
                        //  2. range is empty or
                        //  3. range crossed paragraph boundary
                        // Otherwise, apply as inline property.
                        //if (applyToParagraphs || this.IsEmpty /*|| TextRangeBase.IsParagraphBoundaryCrossed(this)*/)
                        //{
                        //    TextRangeEdit.SetParagraphProperty((TextPointer)textSegment.Start, (TextPointer)textSegment.End, formattingProperty, value, propertyValueAction);
                        //}
                        //else
                        //{
                            TextRangeEdit.SetInlineProperty((TextPointer)textSegment.Start, (TextPointer)textSegment.End, formattingProperty, value, propertyValueAction);
                        //}
                    }
                    else if (TextSchema.IsCharacterProperty(formattingProperty))
                    {
                        TextRangeEdit.SetInlineProperty((TextPointer)textSegment.Start, (TextPointer)textSegment.End, formattingProperty, value, propertyValueAction);
                    }
                    //else if (TextSchema.IsParagraphProperty(formattingProperty))
                    //{
                    //    // We must check for paragraph properties after character ones,
                    //    // to account for overlapping inheritable properties.

                    //    // Thinkness properties (Margin, Padding, BorderThickness) have special treatment
                    //    // in SetParagraphProperty method: it swaps Left and Right values for paragraphs
                    //    // with RightToLeft flow direction. So we need to set them appropriatly -
                    //    // depending on the FlowDirection of the first paragraph.
                    //    if (formattingProperty.PropertyType == typeof(Thickness) &&
                    //        (FlowDirection)textSegment.Start.GetValue(Paragraph.FlowDirectionProperty) == FlowDirection.RightToLeft)
                    //    {
                    //        value = new Thickness(
                    //            ((Thickness)value).Right, ((Thickness)value).Top, ((Thickness)value).Left, ((Thickness)value).Bottom);
                    //    }
                    //    TextRangeEdit.SetParagraphProperty((TextPointer)textSegment.Start, (TextPointer)textSegment.End, formattingProperty, value, propertyValueAction);
                    //}
                }
            }
            finally
            {
                TextRangeBase.EndChange(this);
            }
        }

        // Worker for ClearAllProperties method; enables extensibility for TextSelection
        internal virtual void ClearAllPropertiesVirtual()
        {
            TextRangeBase.BeginChange(this);
            try
            {
                // Clear all inline formattings
                TextRangeEdit.CharacterResetFormatting((TextPointer)this.Start, (TextPointer)this.End);
            }
            finally
            {
                TextRangeBase.EndChange(this);
            }
        }

        //------------------------------------------------------
        //
        //  Table Editing
        //
        //------------------------------------------------------

        // Worker for InsertTable; enables extensibility for TextSelection
        //internal virtual Table InsertTableVirtual(int rowCount, int columnCount)
        //{
        //    TextRangeBase.BeginChange(this);
        //    try
        //    {
        //        return TextRangeEditTables.InsertTable((TextPointer)this.End, rowCount, columnCount);
        //    }
        //    finally
        //    {
        //        TextRangeBase.EndChange(this);
        //    }
        //}

        // Worker for InsertRows; enables extensibility for TextSelection
        //internal virtual TextRange InsertRowsVirtual(int rowCount)
        //{
        //    TextRangeBase.BeginChange(this);
        //    try
        //    {
        //        return TextRangeEditTables.InsertRows(this, rowCount);
        //    }
        //    finally
        //    {
        //        TextRangeBase.EndChange(this);
        //    }
        //}

        // Worker for DeleteRows; enables extensibility for TextSelection
        //internal virtual bool DeleteRowsVirtual()
        //{
        //    TextRangeBase.BeginChange(this);
        //    try
        //    {
        //        return TextRangeEditTables.DeleteRows(this);
        //    }
        //    finally
        //    {
        //        TextRangeBase.EndChange(this);
        //    }
        //}

        // Worker for InsertColumns; enables extensibility for TextSelection
        //internal virtual TextRange InsertColumnsVirtual(int columnCount)
        //{
        //    TextRangeBase.BeginChange(this);
        //    try
        //    {
        //        return TextRangeEditTables.InsertColumns(this, columnCount);
        //    }
        //    finally
        //    {
        //        TextRangeBase.EndChange(this);
        //    }
        //}

        // Worker for DeleteColumns; enables extensibility for TextSelection
        //internal virtual bool DeleteColumnsVirtual()
        //{
        //    TextRangeBase.BeginChange(this);
        //    try
        //    {
        //        return TextRangeEditTables.DeleteColumns(this);
        //    }
        //    finally
        //    {
        //        TextRangeBase.EndChange(this);
        //    }
        //}

        // Worker for MergeCells; enables extensibility for TextSelection
        //internal virtual TextRange MergeCellsVirtual()
        //{
        //    TextRangeBase.BeginChange(this);
        //    try
        //    {
        //        return TextRangeEditTables.MergeCells(this);
        //    }
        //    finally
        //    {
        //        TextRangeBase.EndChange(this);
        //    }
        //}

        // Worker for SplitCells; enables extensibility for TextSelection
        //internal virtual TextRange SplitCellVirtual(int splitCountHorizontal, int splitCountVertical)
        //{
        //    TextRangeBase.BeginChange(this);
        //    try
        //    {
        //        return TextRangeEditTables.SplitCell(this, splitCountHorizontal, splitCountVertical);
        //    }
        //    finally
        //    {
        //        TextRangeBase.EndChange(this);
        //    }
        //}

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties
        
        internal int ChangeBlockLevel
        { 
            get
            {
                return _changeBlockLevel;
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        // Sets boolean state.
        private void SetFlags(bool value, Flags flags)
        {
            _flags = value ? (_flags | flags) : (_flags & (~flags));
        }

        // Reads boolean state.
        private bool CheckFlags(Flags flags)
        {
            return ((_flags & flags) == flags);
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Types
        //
        //------------------------------------------------------

        #region Private Types

        private class ChangeBlock : IDisposable
        {
            internal ChangeBlock(ITextRange range, bool disableScroll)
            {
                _range = range;
                _disableScroll = disableScroll;
                _range.BeginChange();
            }

            void IDisposable.Dispose()
            {
                _range.EndChange(_disableScroll, false /* skipEvents */);
                GC.SuppressFinalize(this);
            }

            private readonly ITextRange _range;
            private readonly bool _disableScroll;
        }

        // Booleans for the _flags field.
        [System.Flags]
        private enum Flags
        {
            // True if normalization should ignore text normalization (surrogates, combining marks, etc).
            // Used for fine-grained control by IMEs.
            IgnoreTextUnitBoundaries = 0x1,

            // True if a Changed event is pending.
            IsChanged = 0x2,

            // True if this range covers a TableCell.
            IsTableCellRange = 0x4,
        }

        #endregion Private Types

        #region Private Fields

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        // A collection of TextSegments. Contains at least one segment.
        private List<TextSegment> _textSegments;

        // Count of nested move sequences.
        private int _changeBlockLevel;

        // Undo unit associated with the current change block, if any.
        private ChangeBlockUndoRecord _changeBlockUndoRecord;

        // Generation id associated with this range. Remembers the state of TextContainer
        // at the moment of the last range building/normalization
        private uint _ContentGeneration;

        // Boolean flags, set with Flags enum.
        private Flags _flags;

        // Boolean flag, set to true via constructor when you want to use the RestrictiveXamlXmlReader  
        private bool _useRestrictiveXamlXmlReader;

        #endregion Private Fields
    }
}
