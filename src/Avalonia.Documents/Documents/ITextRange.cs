// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: A part of abstract layer of TextOM.
//      Defines an abstraction for a TextRange.
//      As the whole abstract TextOM iot supports only read-only
//      positioning operations based on ITextPointer,
//      and a minimal editing capabilities - plain text
//      only.
//      It includes though rich (Xaml) level of serialization
//      support (read-only).
//

namespace System.Windows.Documents
{
    using System.Diagnostics;
    using System.Collections.Generic;
    using System.Threading;
    using System.Globalization;
    using System.IO;
    using Avalonia;

    /// <summary>
    /// A class a portion of text content.
    /// Can be contigous or disjoint; supports rectangular table ranges.
    /// Provides an API for text and table editing operations.
    /// </summary>
    internal interface ITextRange
    {
        //------------------------------------------------------
        //
        // Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        //......................................................
        //
        // Selection Building
        //
        //......................................................

        /// <summary>
        /// Determines if the passed text position is within this range.
        /// </summary>
        /// <param name="position">
        /// The ITextPointer to test.
        /// </param>
        /// <returns>
        /// Returns true if position is contained within this TextRange, false
        /// otherwise.
        /// </returns>
        /// <remarks>
        /// The test is inclusive depending on position's LogicalDirection.
        /// If position == Start and LogicalDirection is Forward
        /// or position == End and LogicalDirection is Backward,
        /// then it is contained by this TextRange.
        /// Empty range does not contain any position.
        /// </remarks>
        bool Contains(ITextPointer position);

        /// <summary>
        /// </summary>
        void Select(ITextPointer position1, ITextPointer position2);

        /// <summary>
        /// Selects a word containing this position
        /// </summary>
        /// <param name="position">
        /// A TextPointer containing a word to select.
        /// </param>
        void SelectWord(ITextPointer position);
        //  Remove this method

        /// <summary>
        /// Selects a paragraph around the given position.
        /// </summary>
        /// <param name="position">
        /// A position identifying a paragraph to select.
        /// </param>
        //void SelectParagraph(ITextPointer position);
        //  Remove this method

        /// <summary>
        /// Adjust the range position in preparation for handling a TextInput event
        /// or equivalent.
        /// </summary>
        /// <param name="overType">
        /// If true, when the range is empty it will delete following character.
        /// Otherwise no content is affected.
        /// </param>
        void ApplyTypingHeuristics(bool overType);

        /// <summary>
        /// Gets the value of the given formatting property on this range.
        /// </summary>
        /// <param name="formattingProperty">
        /// Property value to get.
        /// </param>
        /// <returns>
        /// Value of the requested property.
        /// </returns>
        object GetPropertyValue(AvaloniaProperty formattingProperty);

        /// <summary>
        /// Returns a UIElement if it is selected by this range as its
        /// only content. If there is no UIElement in the range or
        /// if there is any other printable content (charaters, other
        /// UIElements, structural boundaries crossed), the method returns
        /// null.
        /// </summary>
        /// <returns></returns>
        StyledElement GetUIElementSelected();
        //  Think of renaming to something like GetSingleUIElementSelected

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
        //bool CanSave(string dataFormat);

        /// <summary>
        /// Writes the contents of the range into a stream
        /// in a requested format.
        /// </summary>
        /// <param name="stream">
        /// Writeable Stream - destination for the serialized conntent.
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
        /// call CanSave method.
        /// </remarks>
        //void Save(Stream stream, string dataFormat);

        /// <summary>
        /// Writes the contents of the range into a stream
        /// in a requested format.
        /// </summary>
        /// <param name="stream">
        /// Writeable Stream - destination for the serialized conntent.
        /// </param>
        /// <param name="dataFormat">
        /// A string denoting one of supported data conversions:
        /// DataFormats.Text, DataFormats.Xaml, DataFormats.XamlPackage,
        /// DataFormats.Rtf
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
        //void Save(Stream stream, string dataFormat, bool preserveTextElements);

        //  Consider adding CanLoad & Load methods here

        //......................................................
        //
        // Change Notifications
        //
        //......................................................

        /// <summary>
        /// </summary>
        void BeginChange();

        // Like BeginChange, but does not ever create an undo unit.
        // This method is called before UndoManager.Undo, and can't have
        // an open undo unit while running Undo.
        void BeginChangeNoUndo();

        /// <summary>
        /// </summary>
        void EndChange();

        /// <summary>
        /// </summary>
        void EndChange(bool disableScroll, bool skipEvents);

        /// <summary>
        /// </summary>
        IDisposable DeclareChangeBlock();

        /// <summary>
        /// </summary>
        IDisposable DeclareChangeBlock(bool disableScroll);

        /// <summary>
        /// </summary>
        void NotifyChanged(bool disableScroll, bool skipEvents);

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        // If true, normalization will ignore text normalization (surrogates,
        // combining marks, etc).
        // Used for fine-grained control by IMEs.
        bool IgnoreTextUnitBoundaries { get; }

        //......................................................
        //
        //  Boundary Positions
        //
        //......................................................

        /// <summary>
        ///  The starting text position of this TextRange.
        /// </summary>
        ITextPointer Start { get; }

        /// <summary>
        ///  Get the ending text position.
        /// </summary>
        ITextPointer End { get; }

        /// <summary>
        ///  Return true if this text range spans no content
        /// </summary>
        bool IsEmpty { get; }

        /// <summary>
        /// Read-only collection of TextSegments, which is available when
        /// TextRange is in disjoint state.
        /// </summary>
        List<TextSegment> TextSegments { get; }
        //  Make table selection public

        //......................................................
        //
        //  Content - rich and plain
        //
        //......................................................

        // true when this TextRange lives in a TextContainer (as opposed to a
        // generic ITextContainer).
        //
        // This property is used to filter commands that have "richer" meanings
        // when we're acting on a TextContainer.  For instance, we can insert
        // TextElements only to TextContainers, not PasswordTextContainers, etc.
        bool HasConcreteTextContainer { get; }
        //  Use this method in all the code consistently instead of (range.Start is TextPointer) test. Maybe rename the property.

        /// <summary>
        ///  Get and set the text spanned by this text range.
        ///  New line characters and paragraph breaks are
        ///  considered as equivalent from plain text perspective,
        ///  so all kinds of breaks are converted into new lines
        ///  on get, and they converted into paragraph breaks
        ///  on set (if back-end store does allow that, or
        ///  remain new line characters otherwise).
        /// </summary>
        /// <remarks>
        ///  The selected content is collapsed before setting text.
        ///  Collapse assumes mering all block elements crossed by
        ///  this range - from the two neighboring block the preceding
        ///  one survives.
        ///  Character formatting elements are not merged.
        ///  They only eliminated if become empty.
        ///  Range is normalized before inserting a new text,
        ///  which means moving the both ends to the nearest
        ///  caret position in backward direction (as if on typing).
        /// </remarks>
        string Text { get; set; }

        /// <summary>
        /// </summary>
        //string Xml { get; }
        //  Remove this method

        //......................................................
        //
        //  Table Selection Properties
        //
        //......................................................

        //bool IsTableCellRange { get; }
        //  Make table selection public

        //......................................................
        //
        //  Change Notification Properties
        //
        //......................................................

        /// <summary>
        /// Ref count of open change blocks -- incremented/decremented
        /// around BeginChange/EndChange calls.
        /// </summary>
        int ChangeBlockLevel { get; }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Internal Events
        //
        //------------------------------------------------------

        #region Internal Events

        /// <summary>
        /// The Changed event is fired when the range is repositioned
        /// to cover a new span of text.
        ///
        /// The EventHandler delegate is called with this TextRange
        /// as the sender, and EventArgs.Empty as the argument.
        /// </summary>
        event EventHandler Changed;

        /// <summary>
        /// This is for private use only - to access the event
        /// firer from TextRangeBase implementation.
        /// </summary>
        void FireChanged();

        #endregion Internal Events

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        //......................................................
        //  NOTE: These members are technicaly not fields,
        // they are exposed as methods and expected to be implemented
        // by concrete classes.
        // By their nature they belong to "private fields"
        // category - they would be fields if TextRangeBase
        // would be an abstact class.
        // They are supposed to be used only in TextRangeBase,
        // while implementing classes must only provide a storage
        // for them.
        //......................................................

        #region Private Fields

        /// <summary>
        /// Autoincremented counter of content change for the
        /// underlying TextContainer
        /// </summary>
        uint _ContentGeneration { get; set; }

        /// <summary>
        /// Defines a state of this text range
        /// </summary>
        //bool _IsTableCellRange { get; set; }

        /// <summary>
        /// Read-only collection of TextSegments, which is available when
        /// TextRange is in disjoint state.
        /// </summary>
        List<TextSegment> _TextSegments { get; set; }

        /// <summary>
        /// Used by TextRangeBase to track the outermost change block.
        /// </summary>
        int _ChangeBlockLevel { get; set; }

        /// <summary>
        /// Object used by TextRangeBase to track undo status.
        /// </summary>
        ChangeBlockUndoRecord _ChangeBlockUndoRecord { get; set; }

        /// <summary>
        /// </summary>
        bool _IsChanged { get; set; }

        #endregion Public Events
    }
}
