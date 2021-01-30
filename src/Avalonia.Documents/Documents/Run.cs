// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 
// Description: Run class - Text node in Flow content (text run)
//

//using MS.Internal;                  // Invariant.Assert
//using System.Windows.Markup; // ContentProperty
//using System.Windows.Controls;
using Avalonia;
using Avalonia.Data;
using Avalonia.Metadata;
//using MS.Internal.Documents;

namespace System.Windows.Documents
{
    /// <summary>
    /// A terminal element in text flow hierarchy - contains a uniformatted run of unicode characters
    /// </summary>
    public class Run : Inline
    {
        //-------------------------------------------------------------------
        //
        // Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        static Run()
        {
            TextProperty.Changed.AddClassHandler<Run>(OnTextPropertyChanged);
        }

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
        public Run(string text) : this(text, null)
        {
        }

        /// <summary>
        /// Creates a new Run instance.
        /// </summary>
        /// <param name="text">
        /// Optional text content.  May be null.
        /// </param>
        /// <param name="insertionPosition">
        /// Optional position at which to insert the new Run. May
        /// be null.
        /// </param>
        public Run(string text, TextPointer insertionPosition)
        {
            if (insertionPosition != null)
            {
                insertionPosition.TextContainer.BeginChange();
            }
            try
            {
                if (insertionPosition != null)
                {
                    // This will throw InvalidOperationException if schema validity is violated.
                    insertionPosition.InsertInline(this);
                }

                if (text != null)
                {
                    // No need to duplicate the string data in TextProperty here. TextContainer will
                    // set the property to a deferred reference.
                    this.ContentStart.InsertTextInRun(text);
                }
            }
            finally
            {
                if (insertionPosition != null)
                {
                    insertionPosition.TextContainer.EndChange();
                }
            }
        }

        #endregion Constructors

        //-------------------------------------------------------------------
        //
        // Public Properties
        //
        //-------------------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// Dependency property backing Text.
        /// </summary>
        /// <remarks>
        /// Note that when a TextRange that intersects with this Run gets modified (e.g. by editing 
        /// a selection in RichTextBox), we will get two changes to this property since we delete 
        /// and then insert when setting the content of a TextRange.
        /// </remarks>
        public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<Run, string>("Text",
            string.Empty, defaultBindingMode: BindingMode.TwoWay,
            coerce: CoerceText);

        /// <summary>
        /// The content spanned by this TextElement.
        /// </summary>
        [Content]
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        #endregion Public Properties

        //-------------------------------------------------------------------
        //
        // Internal Methods
        //
        //-------------------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Updates TextProperty when it is no longer in sync with the backing store. Called by 
        /// TextContainer when a change affects the text contained by this Run.
        /// </summary>
        /// <remarks>
        /// If a public TextChanged event is added, we need to raise the event only when the
        /// outermost call to this function exits.
        /// </remarks>
        internal override void OnTextUpdated()
        {
            // If the value of Run.Text comes from a local value without a binding expression, we purposely allow the
            // redundant roundtrip property set here. (SetValue on Run.TextProperty causes a TextContainer change,
            // which causes this notification, and we set the property again.) We want to avoid keeping duplicate string
            // data (both in the property system and in the backing store) when Run.Text is set, so we replace the
            // original string property value with a deferred reference. This causes an extra property changed
            // notification, but this is better than duplicating the data.

            //ValueSource textPropertySource = AvaloniaPropertyHelper.GetValueSource(this, TextProperty);
            //if (!_isInsideDeferredSet && (_changeEventNestingCount == 0 || (textPropertySource.BaseValueSource == BaseValueSource.Local
            //    && !textPropertySource.IsExpression)))
            //{
            //    _changeEventNestingCount++;
            //    _isInsideDeferredSet = true;
            //    try
            //    {
            //        // Use a deferred reference as a performance optimization. Most of the time, no
            //        // one will even be watching this property.
            //        SetCurrentDeferredValue(TextProperty, new DeferredRunTextReference(this));
            //    }
            //    finally
            //    {
            //        _isInsideDeferredSet = false;
            //        _changeEventNestingCount--;
            //    }
            //}
        }

        /// <summary>
        /// Increments the reference count that prevents reentrancy during TextContainer changes.
        /// </summary>
        /// <remarks>
        /// Adding/removing elements from the logical tree will cause bindings on Run.Text to get 
        /// invalidated. We don't want to indirectly cause a TextContainer change when that happens.
        /// </remarks>
        internal override void BeforeLogicalTreeChange()
        {
            _changeEventNestingCount++;
        }

        /// <summary>
        /// Decrements the reference count that prevents reentrancy during TextContainer changes.
        /// </summary>
        /// <remarks>
        /// Adding/removing elements from the logical tree will cause bindings on Run.Text to get 
        /// invalidated. We don't want to indirectly cause a TextContainer change when that happens.
        /// </remarks>
        internal override void AfterLogicalTreeChange()
        {
            _changeEventNestingCount--;
        }

        //
        //  This property
        //  1. Finds the correct initial size for the _effectiveValues store on the current IAvaloniaObject
        //  2. This is a performance optimization
        //
        //internal override int EffectiveValuesInitialSize
        //{
        //    get { return 13; }
        //}

        /// <summary>
        /// This method is used by TypeDescriptor to determine if this property should
        /// be serialized.
        /// </summary>
        //[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        //public bool ShouldSerializeText(XamlDesignerSerializationManager manager)
        //{
        //    return manager != null && manager.XmlWriter == null;
        //}

        #endregion Internal Methods


        //-------------------------------------------------------------------
        //
        // Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Changed handler for the Text property.
        /// </summary>
        /// <param name="d">The source of the event.</param>
        /// <param name="e">A PropertyChangedEventArgs that contains the event data.</param>
        /// <remarks>
        /// We can't assume the value is a string here -- it may be a DeferredRunTextReference.
        /// </remarks>
        private static void OnTextPropertyChanged(IAvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
        {
            Run run = (Run)d;

            // Return if this update was caused by a TextContainer change or a reentrant change. 
            if (run._changeEventNestingCount > 0)
            {
                return;
            }

            //Invariant.Assert(!e.NewEntry.IsDeferredReference);

            // CoerceText will have already converted null -> String.Empty, but our default 
            // CoerceValueCallback could be overridden by a derived class.  So check again here.
            string newText = (string)e.NewValue;
            if (newText == null)
            {
                newText = String.Empty;
            }

            // Run.TextProperty has changed. Update the backing store.
            run._changeEventNestingCount++;
            try
            {
                TextContainer textContainer = run.TextContainer;
                textContainer.BeginChange();

                try
                {
                    TextPointer contentStart = run.ContentStart;
                    if (!run.IsEmpty)
                    {
                        textContainer.DeleteContentInternal(contentStart, run.ContentEnd);
                    }
                    contentStart.InsertTextInRun(newText);
                }
                finally
                {
                    textContainer.EndChange();
                }
            }
            finally
            {
                run._changeEventNestingCount--;
            }

            // We need to clear undo stack if we are in a RichTextBox and the value comes from
            // data binding or some other expression.
            //FlowDocument document = run.TextContainer.Parent as FlowDocument;
            //if (document != null)
            //{
            //    RichTextBox rtb = document.Parent as RichTextBox;
            //    if (rtb != null && run.HasExpression(run.LookupEntry(Run.TextProperty.GlobalIndex), Run.TextProperty))
            //    {
            //        UndoManager undoManager = rtb.TextEditor._GetUndoManager();
            //        if (undoManager != null && undoManager.IsEnabled)
            //        {
            //            undoManager.Clear();
            //        }
            //    }
            //}
        }

        /// <summary>
        /// Coercion callback for the Text property.
        /// </summary>
        /// <param name="d">The object that the property exists on.</param>
        /// <param name="baseValue">The new value of the property, prior to any coercion attempt.</param>
        /// <returns>The coerced value.</returns>
        /// <remarks>
        /// We can't assume the value is a string here -- it may be a DeferredRunTextReference.
        /// </remarks>
        private static string CoerceText(IAvaloniaObject d, string baseValue)
        {
            if (baseValue == null)
            {
                baseValue = string.Empty;
            }

            return baseValue;
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // Number of nested TextContainer change notifications.
        private int _changeEventNestingCount;

        // If we are inside a property set caused by a backing store change.
        //private bool _isInsideDeferredSet;

        #endregion Private Fields
    }
}
