// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

// Description: A static class that manipulates an array TextTreeTextBlocks.
//

using System;
using System.Collections;
using MS.Internal;

namespace System.Windows.Documents
{
    // Each TextContainer maintains an array of TextTreeTextBlocks that holds all
    // the raw text in the tree.  This class manipulates that array.
    //
    // "Raw text" includes not only unicode covered by TextTreeTextNodes, but
    // also placeholders for element edges and embedded objects.  Inserting
    // placeholders lets us map 1-to-1 with array offsets and symbol offsets.
    internal static class TextTreeText
    {
        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        // Inserts text into the text block array.  The text is either a string
        // or an array of char.
        internal static void InsertText(TextTreeRootTextBlock rootTextBlock, int offset, object text)
        {
            TextTreeTextBlock block;
            int localOffset;
            int insertCount;
            int textLength;

            Invariant.Assert(text is string || text is char[], "Bad text parameter!");

            // Get the block matching the insertion point.
            block = FindBlock(rootTextBlock, offset, out localOffset);

            // Fill this block to capacity.
            textLength = TextContainer.GetTextLength(text);
            insertCount = block.InsertText(localOffset, text, 0, textLength);

            if (insertCount < textLength)
            {
                // Put all the text to the smaller side of the gap into the new block.
                if (block.GapOffset < TextTreeTextBlock.MaxBlockSize / 2)
                {
                    InsertTextLeft(block, text, insertCount);
                }
                else
                {
                    InsertTextRight(block, text, insertCount);
                }
            }
        }

        // Removes text from the block array.
        // consider merging blocks after the remove.  Not yet clear if
        // this is desirable. 
        internal static void RemoveText(TextTreeRootTextBlock rootTextBlock, int offset, int count)
        {
            int firstBlockLocalOffset;
            TextTreeTextBlock firstBlock;
            int lastBlockLocalOffset;
            TextTreeTextBlock lastBlock;
            SplayTreeNode firstRemoveBlock;
            SplayTreeNode lastRemoveBlock;
            int firstCount;

            if (count == 0)
            {
                // Early out on count == 0 so we don't get in trouble at the
                // very end of the array.
                return;
            }

            // Get the block matching the offset.
            firstBlock = FindBlock(rootTextBlock, offset, out firstBlockLocalOffset);

            if (firstBlock.Count == firstBlockLocalOffset)
            {
                // FindIndexForOffset always returns the lower block if we ask
                // for a cp between two blocks.
                // For a remove, we want to work with the following block, which
                // actually contains the content.
                firstBlock = (TextTreeTextBlock)firstBlock.GetNextNode();
                Invariant.Assert(firstBlock != null);
                firstBlockLocalOffset = 0;
            }

            // And the block matching the offset + count.
            lastBlock = FindBlock(rootTextBlock, offset + count, out lastBlockLocalOffset);

            if (firstBlockLocalOffset > 0 || count < firstBlock.Count)
            {
                // Remove text from the first block.
                firstCount = Math.Min(count, firstBlock.Count - firstBlockLocalOffset);
                firstBlock.RemoveText(firstBlockLocalOffset, firstCount);
                // Don't remove the first block, since some text was left behind.
                firstRemoveBlock = firstBlock.GetNextNode();
            }
            else
            {
                // All text in the first block covered -- just remove it entirely.
                firstCount = 0;
                firstRemoveBlock = firstBlock;
            }

            if (count > firstCount)
            {
                int lastCount;

                if (lastBlockLocalOffset < lastBlock.Count)
                {
                    lastCount = lastBlockLocalOffset;
                    // Remove some text.
                    lastBlock.RemoveText(0, lastBlockLocalOffset);
                    // There's text left over in the last block, so don't remove
                    // the block.
                    lastRemoveBlock = lastBlock.GetPreviousNode();
                }
                else
                {
                    lastCount = 0;
                    // All text in the last block covered -- just remove it entirely.
                    lastRemoveBlock = lastBlock;
                }

                // If firstRemoveBlock == lastBlock && lastRemoveBlock == firstBlock,
                // then there are no more blocks to remove -- we removed a portion
                // from the first and last block and they are direct neighbors.
                if (firstCount + lastCount < count)
                {
                    // Remove any blocks in the middle of first, last.
                    Remove((TextTreeTextBlock)firstRemoveBlock, (TextTreeTextBlock)lastRemoveBlock);
                }
            }
        }

        // Remove text from the block array, and return the removed text in a
        // char array.
        internal static char[] CutText(TextTreeRootTextBlock rootTextBlock, int offset, int count)
        {
            char[] text;

            text = new char[count];

            ReadText(rootTextBlock, offset, count, text, 0);
            RemoveText(rootTextBlock, offset, count);

            return text;
        }

        // Read text in the block array.
        internal static void ReadText(TextTreeRootTextBlock rootTextBlock, int offset, int count, char[] chars, int startIndex)
        {
            TextTreeTextBlock block;
            int localOffset;
            int blockCount;

            if (count > 0)
            {
                // Get the block matching the offset.
                block = FindBlock(rootTextBlock, offset, out localOffset);

                while (true)
                {
                    Invariant.Assert(block != null, "Caller asked for too much text!");
                    blockCount = block.ReadText(localOffset, count, chars, startIndex);
                    localOffset = 0;
                    count -= blockCount;
                    if (count == 0)
                        break;

                    startIndex += blockCount;
                    block = (TextTreeTextBlock)block.GetNextNode();
                }
            }
        }

        // Inserts a placeholder character for an embedded object.
        // The actual value stored doesn't really matter, it will never be read.
        internal static void InsertObject(TextTreeRootTextBlock rootTextBlock, int offset)
        {
            InsertText(rootTextBlock, offset, new string((char)0xffff, 1));
        }

        // Insert placeholders for elements edges into the block array.
        // The actual value stored doesn't really matter, it will never be read.
        internal static void InsertElementEdges(TextTreeRootTextBlock rootTextBlock, int offset, int childSymbolCount)
        {
            if (childSymbolCount == 0)
            {
                InsertText(rootTextBlock, offset, new string((char)0xbeef, 2));
            }
            else
            {
                InsertText(rootTextBlock, offset, new string((char)0xbeef, 1));
                InsertText(rootTextBlock, offset + childSymbolCount + 1, new string((char)0x0, 1));
            }
        }

        // Remove placeholder element edge characters from the block array.
        internal static void RemoveElementEdges(TextTreeRootTextBlock rootTextBlock, int offset, int symbolCount)
        {
            Invariant.Assert(symbolCount >= 2, "Element must span at least two symbols!"); // 2 element edges == 2 symbols.
            
            if (symbolCount == 2)
            {
                RemoveText(rootTextBlock, offset, 2);
            }
            else
            {
                RemoveText(rootTextBlock, offset + symbolCount - 1, 1);
                RemoveText(rootTextBlock, offset, 1);
            }
        }

        #endregion Internal methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        // Finds the TextTreeTextBlock that contains the specified char offset.
        // Returns the lower block for offsets that border two blocks.
        private static TextTreeTextBlock FindBlock(TextTreeRootTextBlock rootTextBlock, int offset, out int localOffset)
        {
            TextTreeTextBlock node;
            int nodeOffset;

            node = (TextTreeTextBlock)rootTextBlock.ContainedNode.GetSiblingAtOffset(offset, out nodeOffset);

            // If offset is between two blocks, make sure we return the lower of the two.
            if (node.LeftSymbolCount == offset)
            {
                TextTreeTextBlock previousBlock = (TextTreeTextBlock)node.GetPreviousNode();
                if (previousBlock != null)
                {
                    node = previousBlock;
                    nodeOffset -= node.SymbolCount;
                    Invariant.Assert(nodeOffset >= 0);
                }
            }

            localOffset = offset - nodeOffset;
            Invariant.Assert(localOffset >= 0 && localOffset <= node.Count);

            return node;
        }

        // Helper for InsertText.  Inserts text to the left of an existing block.
        private static void InsertTextLeft(TextTreeTextBlock rightBlock, object text, int textOffset)
        {
            int newBlockCount;
            TextTreeTextBlock leftBlock;
            TextTreeTextBlock neighborBlock;
            TextTreeTextBlock newBlock;
            int count;
            int textEndOffset = -1;
            int i;
            int length;

            length = TextContainer.GetTextLength(text);

            if (rightBlock.GapOffset == 0)
            {
                // Try to fill neighbor block.
                neighborBlock = (TextTreeTextBlock)rightBlock.GetPreviousNode();
                if (neighborBlock != null)
                {
                    textOffset += neighborBlock.InsertText(neighborBlock.Count, text, textOffset, length);
                }
            }

            if (textOffset < length)
            {
                // Try adding just one block.
                newBlockCount = 1;

                leftBlock = rightBlock.SplitBlock();

                // Fill up the left block.
                textOffset += leftBlock.InsertText(leftBlock.Count, text, textOffset, length);

                if (textOffset < length)
                {
                    // Fill up the larger block.
                    // We need to copy from the end of the text here.
                    count = Math.Min(rightBlock.FreeCapacity, length - textOffset);
                    textEndOffset = length - count;
                    rightBlock.InsertText(0, text, textEndOffset, length);

                    if (textOffset < textEndOffset)
                    {
                        // We've filled both blocks, and there's still more text to copy.
                        // Prepare to allocate some more blocks.
                        newBlockCount += (textEndOffset - textOffset + TextTreeTextBlock.MaxBlockSize - 1) / TextTreeTextBlock.MaxBlockSize;
                    }
                }

                for (i = 1; i < newBlockCount; i++)
                {
                    newBlock = new TextTreeTextBlock(TextTreeTextBlock.MaxBlockSize);
                    textOffset += newBlock.InsertText(0, text, textOffset, textEndOffset);
                    newBlock.InsertAtNode(leftBlock, false /* insertBefore */);
                    leftBlock = newBlock;
                }
                Invariant.Assert(newBlockCount == 1 || textOffset == textEndOffset, "Not all text copied!");
            }
        }

        // Helper for InsertText.  Inserts text to the right of an existing block.
        private static void InsertTextRight(TextTreeTextBlock leftBlock, object text, int textOffset)
        {
            int newBlockCount;
            TextTreeTextBlock rightBlock;
            TextTreeTextBlock neighborBlock;
            TextTreeTextBlock newBlock;
            int count;
            int textEndOffset;
            int i;

            textEndOffset = TextContainer.GetTextLength(text);

            if (leftBlock.GapOffset == leftBlock.Count)
            {
                // Try to fill neighbor block.
                neighborBlock = (TextTreeTextBlock)leftBlock.GetNextNode();
                if (neighborBlock != null)
                {
                    count = Math.Min(neighborBlock.FreeCapacity, textEndOffset - textOffset);
                    neighborBlock.InsertText(0, text, textEndOffset - count, textEndOffset);
                    textEndOffset -= count;
                }
            }
            
            if (textOffset < textEndOffset)
            {
                // Try adding just one block.
                newBlockCount = 1;

                rightBlock = leftBlock.SplitBlock();

                // Fill up the right block.
                count = Math.Min(rightBlock.FreeCapacity, textEndOffset - textOffset);
                rightBlock.InsertText(0, text, textEndOffset - count, textEndOffset);
                textEndOffset -= count;

                if (textOffset < textEndOffset)
                {
                    // Fill up the larger block.
                    // We need to copy from the end of the text here.
                    textOffset += leftBlock.InsertText(leftBlock.Count, text, textOffset, textEndOffset);

                    if (textOffset < textEndOffset)
                    {
                        // We've filled both blocks, and there's still more text to copy.
                        // Prepare to allocate some more blocks.
                        newBlockCount += (textEndOffset - textOffset + TextTreeTextBlock.MaxBlockSize - 1) / TextTreeTextBlock.MaxBlockSize;
                    }
                }

                for (i=0; i<newBlockCount-1; i++)
                {
                    newBlock = new TextTreeTextBlock(TextTreeTextBlock.MaxBlockSize);
                    textOffset += newBlock.InsertText(0, text, textOffset, textEndOffset);
                    newBlock.InsertAtNode(leftBlock, false /* insertBefore */);
                    leftBlock = newBlock;
                }
                Invariant.Assert(textOffset == textEndOffset, "Not all text copied!");
            }
        }

        // Removes a run of nodes from a tree.
        internal static void Remove(TextTreeTextBlock firstNode, TextTreeTextBlock lastNode)
        {
            SplayTreeNode leftTree;
            SplayTreeNode rightTree;
            SplayTreeNode rootNode;
            SplayTreeNode containerNode;

            //
            // Break the tree into three subtrees.
            //

            leftTree = firstNode.GetPreviousNode();
            if (leftTree != null)
            {
                // Splitting moves leftTree to local root.
                leftTree.Split();
                containerNode = leftTree.ParentNode;
                leftTree.ParentNode = null; // We'll fixup leftTree.ParentNode.ContainedNode below.
                // Join requires that leftTree has a null ParentNode.
            }
            else
            {
                // There are no preceeding nodes.
                containerNode = firstNode.GetContainingNode();
            }

            rightTree = lastNode.Split();

            //
            // Recombine the two outer trees.
            //
            rootNode = SplayTreeNode.Join(leftTree, rightTree);

            if (containerNode != null)
            {
                containerNode.ContainedNode = rootNode;
            }
            if (rootNode != null)
            {
                rootNode.ParentNode = containerNode;
            }
        }

        internal static void ReadText(object rootTextBlock, int symbolOffset, int finalCount, char[] textBuffer, int startIndex)
        {
            throw new NotImplementedException();
        }

        #endregion Private methods
    }
}
