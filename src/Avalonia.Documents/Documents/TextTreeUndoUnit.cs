// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Base class for all TextContainer undo units.
//

using System;
using MS.Internal;
using MS.Internal.Documents;
//using System.Windows.Data;
using Avalonia;

namespace System.Windows.Documents
{
    // Base class for all TextContainer undo units.
    internal abstract class TextTreeUndoUnit : IUndoUnit
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        // Create a new undo unit instance.
        internal TextTreeUndoUnit(TextContainer tree, int symbolOffset)
        {
            _tree = tree;
            _symbolOffset = symbolOffset;
            _treeContentHashCode = _tree.GetContentHashCode();
        }

        #endregion Constructors
 
        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        // Called by the undo manager.  Restores tree state to its condition
        // when the unit was created.  Assumes the tree state matches conditions
        // just after the unit was created.
        public void Do()
        {
            _tree.BeginChange();
            try
            {
                DoCore();
            }
            finally
            {
                _tree.EndChange();
            }
        }

        // Worker for Do method, implemented by derived class.
        public abstract void DoCore();

        // Called by the undo manager.  TextContainer undo units never merge.
        public bool Merge(IUndoUnit unit)
        {
            Invariant.Assert(unit != null);
            return false;
        }

        #endregion Public Methods        

        //------------------------------------------------------
        //
        //  Protected Properties
        //
        //------------------------------------------------------

        #region Protected Properties

        // TextContainer associated with this undo unit.
        protected TextContainer TextContainer
        {
            get { return _tree; }
        }

        // Offset in symbols of this undo unit within the TextContainer content.
        protected int SymbolOffset
        {
            get { return _symbolOffset; }
        }

        #endregion Protected Properties

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        // Explicitly sets this undo unit's content hash code to match the
        // current tree state.  This happens automatically in the ctor, but
        // some undo units (for delte operations) need to be initialized before
        // the content is modified, in which case they call this method
        // afterwards.
        internal void SetTreeHashCode()
        {
            _treeContentHashCode = _tree.GetContentHashCode();
        }

        // Verifies the TextContainer state matches the original state when this undo
        // unit was created.  Because we use symbol offsets to track the
        // position of the content to modify, errors in the undo code or reentrant
        // document edits by random code could potentially
        // corrupt data rather than raise an immediate exception.
        //
        // This method uses Invariant.Assert to trigger a FailFast in the case an error
        // is detected, before we get a chance to corrupt data.
        internal void VerifyTreeContentHashCode()
        {
            if (_tree.GetContentHashCode() != _treeContentHashCode)
            {
                // Data is irrecoverably corrupted, shut down!
                Invariant.Assert(false, "Undo unit is out of sync with TextContainer!");
            }
        }

        // Gets an array of PropertyRecords from a IAvaloniaObject's LocalValueEnumerator.
        // The array is safe to cache, LocalValueEnumerators are not.
        internal static PropertyRecord[] GetPropertyRecordArray(IAvaloniaObject d)
        {
            LocalValueEnumerator valuesEnumerator = d.GetLocalValueEnumerator();
            PropertyRecord[] records = new PropertyRecord[valuesEnumerator.Count];
            int count = 0;

            valuesEnumerator.Reset();
            while (valuesEnumerator.MoveNext())
            {
                AvaloniaProperty dp = valuesEnumerator.Current.Property;
                if (!dp.IsReadOnly)
                {
                    // LocalValueEntry.Value can be an Expression, which we can't duplicate when we 
                    // undo, so we copy over the current value from IAvaloniaObject.GetValue instead.
                    records[count].Property = dp;
                    records[count].Value = d.GetValue(dp);

                    count++;
                }
            }

            PropertyRecord[] trimmedResult;
            if(valuesEnumerator.Count != count)
            {
                trimmedResult = new PropertyRecord[count];
                for(int i=0; i<count; i++)
                {
                    trimmedResult[i] = records[i];
                }
            }
            else
            {
                trimmedResult = records;
            }

            return trimmedResult;
        }

        // Converts array of PropertyRecords into a LocalValueEnumerator.
        // The array is safe to cache, LocalValueEnumerators are not.
        internal static LocalValueEnumerator ArrayToLocalValueEnumerator(PropertyRecord[] records)
        {
            IAvaloniaObject obj;
            int i;

            obj = new AvaloniaObject();

            for (i = 0; i < records.Length; i++)
            {
                obj.SetValue(records[i].Property, records[i].Value);
            }

            return obj.GetLocalValueEnumerator();
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // The tree associated with this undo unit.
        private readonly TextContainer _tree;

        // Offset in the tree at which to undo.
        private readonly int _symbolOffset;

        // Hash representing the state of the tree when the undo unit was
        // created.  If the hash doesn't match when Do is called, there's a bug
        // somewhere, and any TextContainer undo units on the stack are probably
        // corrupted.
        private int _treeContentHashCode;

        #endregion Private Fields
    }
}

