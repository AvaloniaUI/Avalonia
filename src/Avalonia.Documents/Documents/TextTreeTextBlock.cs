// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: A block of text stored in a TextContainer.
//
//

using System;
using MS.Internal;

namespace System.Windows.Documents
{
    // Each TextContainer maintains an array of TextTreeTextBlocks that holds all
    // the raw text in the tree.
    //
    // TextTreeTextBlocks are simple char arrays with some extra state that
    // tracks current char count vs capacity.  Instead of simply storing
    // text at the head of the array, we use the "buffer gap" algorithm.
    // Free space in the array always follows the last insertion, so with
    // sequential writes we don't need to memmove any existing text.
    internal class TextTreeTextBlock : SplayTreeNode
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        // Create a new TextTreeTextBlock instance.
        internal TextTreeTextBlock(int size)
        {
            Invariant.Assert(size > 0);
            Invariant.Assert(size <= MaxBlockSize);

            _text = new char[size];
            _gapSize = size;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

#if DEBUG
        // Debug-only ToString override.
        public override string ToString()
        {
            return ("TextTreeTextBlock Id=" + this.DebugId + " Count=" + this.Count);
        }
#endif // DEBUG

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        // Inserts text into the block, up to the remaining block capacity.
        // Returns the number of chars actually inserted.
        internal int InsertText(int logicalOffset, object text, int textStartIndex, int textEndIndex)
        {
            int count;
            string textString;
            char[] textChars;
            char[] newText;
            int rightOfGapLength;

            Invariant.Assert(text is string || text is char[], "Bad text parameter!");
            Invariant.Assert(textStartIndex <= textEndIndex, "Bad start/end index!");

            // Splay this node so we don't invalidate any LeftSymbolCounts.
            Splay();

            count = textEndIndex - textStartIndex;

            if (_text.Length < MaxBlockSize && count > _gapSize)
            {
                // We need to grow this block.
                // We're very conservative here, allocating no more than the
                // caller asks for.  Once we push past the MaxBlockSize, we'll
                // be more aggressive with allocations.
                newText = new char[Math.Min(this.Count + count, MaxBlockSize)];
                Array.Copy(_text, 0, newText, 0, _gapOffset);
                rightOfGapLength = _text.Length - (_gapOffset + _gapSize);
                Array.Copy(_text, _gapOffset + _gapSize, newText, newText.Length - rightOfGapLength, rightOfGapLength);
                _gapSize += newText.Length - _text.Length;
                _text = newText;
            }

            // Move the gap to the insert point.
            if (logicalOffset != _gapOffset)
            {
                MoveGap(logicalOffset);
            }

            // Truncate the copy.
            count = Math.Min(count, _gapSize);

            textString = text as string;
            if (textString != null)
            {
                // Do the work.
                textString.CopyTo(textStartIndex, _text, logicalOffset, count);
            }
            else
            {
                textChars = (char[])text;

                // Do the work.
                Array.Copy(textChars, textStartIndex, _text, logicalOffset, count);
            }

            // Update the gap.            
            _gapOffset += count;
            _gapSize -= count;

            return count;
        }

        // Splits this block at the current gap offset.
        // Only called during a text insert, when the block is full.
        // If GapOffset < TextTreeTextBlock.MaxBlockSize / 2, returns
        // a new block with the left text, otherwise returns a new
        // block with the right text.
        internal TextTreeTextBlock SplitBlock()
        {
            TextTreeTextBlock newBlock;
            bool insertBefore;

            Invariant.Assert(_gapSize == 0, "Splitting non-full block!");
            Invariant.Assert(_text.Length == MaxBlockSize, "Splitting non-max sized block!");

            newBlock = new TextTreeTextBlock(MaxBlockSize);

            if (_gapOffset < MaxBlockSize / 2)
            {
                // Copy the left text over to the new block.
                Array.Copy(_text, 0, newBlock._text, 0, _gapOffset);
                newBlock._gapOffset = _gapOffset;
                newBlock._gapSize = MaxBlockSize - _gapOffset;

                // Remove the left text from this block.
                _gapSize += _gapOffset;
                _gapOffset = 0;

                // New node preceeds this one.
                insertBefore = true;
            }
            else
            {
                // Copy the right text over to the new block.
                Array.Copy(_text, _gapOffset, newBlock._text, _gapOffset, MaxBlockSize - _gapOffset);
                Invariant.Assert(newBlock._gapOffset == 0);
                newBlock._gapSize = _gapOffset;
                
                // Remove the left text from this block.
                _gapSize = MaxBlockSize - _gapOffset;

                // New node follows this one.
                insertBefore = false;
            }

            // Add the new node to the splay tree.
            newBlock.InsertAtNode(this, insertBefore);

            return newBlock;
        }

        // Removes text at a logical offset (an offset that does not
        // consider the gap).
        internal void RemoveText(int logicalOffset, int count)
        {
            int precedingTextToRemoveCount;

            Invariant.Assert(logicalOffset >= 0);
            Invariant.Assert(count >= 0);
            Invariant.Assert(logicalOffset + count <= this.Count, "Removing too much text!");

            int originalCountToRemove = count;
            int originalCount = this.Count;

            // Splay this node so we don't invalidate any LeftSymbolCounts.
            Splay();

            // REVIEW:benwest: this looks way over complicated.
            // Couldn't we just move to gap to offset and then
            // extend it, in all cases?

            // Remove text before the gap.
            if (logicalOffset < _gapOffset)
            {
                if (logicalOffset + count < _gapOffset)
                {
                    // Shift text over.
                    MoveGap(logicalOffset + count);
                }

                // Extend the gap to "remove" the text.                
                precedingTextToRemoveCount = (logicalOffset + count == _gapOffset) ? count : _gapOffset - logicalOffset;
                _gapOffset -= precedingTextToRemoveCount;
                _gapSize += precedingTextToRemoveCount;
                
                // Adjust logicalOffset, count, so that they follow the gap below.
                logicalOffset = _gapOffset;
                count -= precedingTextToRemoveCount;
            }
            
            // Make offset relative to text after the gap.
            logicalOffset += _gapSize;
            
            // Remove text after the gap.
            if (logicalOffset > _gapOffset + _gapSize)
            {
                // Shift text over.
                MoveGap(logicalOffset - _gapSize);
            }
            
            // Extend the gap to "remove" the text.
            _gapSize += count;
            Invariant.Assert(_gapOffset + _gapSize <= _text.Length);
            Invariant.Assert(originalCount == this.Count + originalCountToRemove);
        }

        // Copies text into a char array, returns the actual count of chars copied,
        // which may be smaller than count if the end of block is encountered.
        internal int ReadText(int logicalOffset, int count, char []chars, int charsStartIndex)
        {
            int copyCount;
            int originalCount;
            
            originalCount = count;

            // Read text before the gap.
            if (logicalOffset < _gapOffset)
            {
                copyCount = Math.Min(count, _gapOffset - logicalOffset);
                Array.Copy(_text, logicalOffset, chars, charsStartIndex, copyCount);
                count -= copyCount;
                charsStartIndex += copyCount;
                
                // Adjust logicalOffset, so that it will follow the gap below.
                logicalOffset = _gapOffset;
            }

            if (count > 0)
            {
                // Make offset relative to text after the gap.
                logicalOffset += _gapSize;
                
                // Read the text following the gap.
                copyCount = Math.Min(count, _text.Length - logicalOffset);
                Array.Copy(_text, logicalOffset, chars, charsStartIndex, copyCount);
                count -= copyCount;
            }
            
            return (originalCount - count);
        }
        
        #endregion Internal methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        // If this node is a local root, then ParentNode contains it.
        // Otherwise, this is the node parenting this node within its tree.
        internal override SplayTreeNode ParentNode
        {
            get
            {
                return _parentNode;
            }

            set
            {
                _parentNode = value;
            }
        }

        // TextTreeTextNode never has contained nodes.
        internal override SplayTreeNode ContainedNode
        {
            get
            {
                return null;
            }

            set
            {
                Invariant.Assert(false, "Can't set ContainedNode on a TextTreeTextBlock!");
            }
        }

        // Count of symbols of all siblings preceding this node.
        internal override int LeftSymbolCount
        {
            get
            {
                return _leftSymbolCount;
            }

            set
            {
                _leftSymbolCount = value;
            }
        }

        // Count of unicode chars of all siblings preceding this node.
        // This property is only used by TextTreeNodes.
        internal override int LeftCharCount
        {
            get
            {
                return 0;
            }

            set
            {
                Invariant.Assert(value == 0);
            }
        }

        // Left child node in a sibling tree.
        internal override SplayTreeNode LeftChildNode
        {
            get
            {
                return _leftChildNode;
            }

            set
            {
                _leftChildNode = (TextTreeTextBlock)value;
            }
        }

        // Right child node in a sibling tree.
        internal override SplayTreeNode RightChildNode
        {
            get
            {
                return _rightChildNode;
            }

            set
            {
                _rightChildNode = (TextTreeTextBlock)value;
            }
        }

        // Unused by this derived class.
        internal override uint Generation
        {
            get
            {
                return 0;
            }

            set
            {
                Invariant.Assert(false, "TextTreeTextBlock does not track Generation!");
            }
        }

        // Cached symbol offset.
        // Unused by this derived class.
        internal override int SymbolOffsetCache
        {
            get
            {
                return -1;
            }

            set
            {
                Invariant.Assert(false, "TextTreeTextBlock does not track SymbolOffsetCache!");
            }
        }

        // Count of symbols covered by this node.
        internal override int SymbolCount
        {
            get
            {
                return this.Count;
            }

            set
            {
                Invariant.Assert(false, "Can't set SymbolCount on TextTreeTextBlock!");
            }
        }

        // Count of unicode chars covered by this node and any contained nodes.
        // This property is only used by TextTreeNodes.
        internal override int IMECharCount
        {
            get
            {
                return 0;
            }

            set
            {
                Invariant.Assert(value == 0);
            }
        }

        // The number of chars stored in this block.
        internal int Count
        {
            get
            {
                return (_text.Length - _gapSize);
            }
        }
        
        // The number of additional chars this Block could accept before running out of space.
        internal int FreeCapacity
        {
            get
            {
                return _gapSize;
            }
        }

        // The offset of the gap in this block.        
        internal int GapOffset
        {
            get
            {
                return _gapOffset;
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        // Repositions the gap to a new offset, shifting text as necessary.
        private void MoveGap(int offset)
        {
            int sourceOffset;
            int destinationOffset;
            int count;
            
            if (offset < _gapOffset)
            {
                sourceOffset = offset;
                destinationOffset = offset + _gapSize;
                count = _gapOffset - offset;
            }
            else
            {
                sourceOffset = _gapOffset + _gapSize;
                destinationOffset = _gapOffset;
                count = offset - _gapOffset;
            }
            
            Array.Copy(_text, sourceOffset, _text, destinationOffset, count);
            _gapOffset = offset;
        }
 
        #endregion Private methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // Count of symbols of all siblings preceding this node.
        private int _leftSymbolCount;

        // The TextTreeTextBlock parenting this node within its tree,
        // or the TextTreeRootTextBlock containing this node.
        private SplayTreeNode _parentNode;

        // Left child node in a sibling tree.
        private TextTreeTextBlock _leftChildNode;

        // Right child node in a sibling tree.
        private TextTreeTextBlock _rightChildNode;

        // An array of text in the block.
        private char []_text;
        
        // Position of the buffer gap.
        private int _gapOffset;
        
        // Size of the buffer gap.
        private int _gapSize;

        // Max block size, in chars.
        internal const int MaxBlockSize = 4096;        

        #endregion Private Fields
    }
}
