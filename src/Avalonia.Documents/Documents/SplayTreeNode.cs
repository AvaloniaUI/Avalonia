// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Base class for all nodes in a TextContainer or TextBlock splay
//              tree.
//
using MS.Internal;

namespace System.Windows.Documents
{
    // Base class for all nodes in a TextContainer or TextBlock splay tree.
    internal abstract class SplayTreeNode
    {
        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        // Returns the SplayTreeNode at a symbol offset relative to this node.
        // On return, nodeOffset holds the relative offset of the node start
        // (the node may span several symbols surrounding the desired offset).
        internal SplayTreeNode GetSiblingAtOffset(int offset, out int nodeOffset)
        {
            SplayTreeNode node;
            int nodeSymbolCount;
            int nodeLeftSymbolCount;

            node = this;
            nodeOffset = 0;

            while (true)
            {
                nodeLeftSymbolCount = node.LeftSymbolCount;

                if (offset < nodeOffset + nodeLeftSymbolCount)
                {
                    // This node is to the right of the one we're looking for.
                    node = node.LeftChildNode;
                }
                else
                {
                    nodeSymbolCount = node.SymbolCount;

                    if (offset <= nodeOffset + nodeLeftSymbolCount + nodeSymbolCount)
                    {
                        // We're somewhere inside this node.
                        nodeOffset += nodeLeftSymbolCount;
                        break;
                    }
                    else
                    {
                        // This node is to the left of the one we're looking for.
                        nodeOffset += nodeLeftSymbolCount + nodeSymbolCount;
                        node = node.RightChildNode;
                    }
                }
            }

            // Splay the found node.  This pulls the node up to the root and
            // gives us amortized constant time for sequential accesses.
            node.Splay();

            return node;
        }

        // Returns the SplayTreeNode at a char offset relative to this node.
        // On return, nodeCharOffset holds the relative char offset of the node start
        // (the node may span several symbols surrounding the desired char offset).
        internal SplayTreeNode GetSiblingAtCharOffset(int charOffset, out int nodeCharOffset)
        {
            SplayTreeNode node;
            int nodeCharCount;
            int nodeLeftCharCount;

            node = this;
            nodeCharOffset = 0;

            while (true)
            {
                nodeLeftCharCount = node.LeftCharCount;

                if (charOffset < nodeCharOffset + nodeLeftCharCount)
                {
                    // This node is to the right of the one we're looking for.
                    node = node.LeftChildNode;
                }
                else if (charOffset == nodeCharOffset + nodeLeftCharCount &&
                         charOffset > 0)
                {
                    // This node starts at the desired char offset.
                    // But we want instead to continue searching for the node
                    // that ends at the char offset.
                    node = node.LeftChildNode;
                }
                else
                {
                    nodeCharCount = node.IMECharCount;

                    if (nodeCharCount > 0 &&
                        charOffset <= nodeCharOffset + nodeLeftCharCount + nodeCharCount)
                    {
                        // We're somewhere inside this node.
                        nodeCharOffset += nodeLeftCharCount;
                        break;
                    }
                    else
                    {
                        // This node is to the left of the one we're looking for.
                        nodeCharOffset += nodeLeftCharCount + nodeCharCount;
                        node = node.RightChildNode;
                    }
                }
            }

            // Splay the found node.  This pulls the node up to the root and
            // gives us amortized constant time for sequential accesses.
            node.Splay();

            return node;
        }

        // Returns the first (lowest symbol offset) logical child of a node, if
        // any.  Only TextTreeRootNode and TextTreeTextElement nodes
        // ever have contained nodes.
        internal SplayTreeNode GetFirstContainedNode()
        {
            SplayTreeNode containedNode;

            containedNode = this.ContainedNode;

            if (containedNode == null)
                return null;

            return containedNode.GetMinSibling();
        }

        // Returns the last (highest symbol offset) logical child of a node, if
        // any.  Only TextTreeRootNode and TextTreeTextElement nodes
        // ever have contained nodes.
        internal SplayTreeNode GetLastContainedNode()
        {
            SplayTreeNode containedNode;

            containedNode = this.ContainedNode;

            if (containedNode == null)
                return null;

            return containedNode.GetMaxSibling();
        }

        // Returns a node's containing node.  If the node is a TextTreeRootNode,
        // returns null.
        internal SplayTreeNode GetContainingNode()
        {
            // Splaying moves this node up to the local root, so
            // the containing node must, afterwards, be our parent.
            //
            // Splaying here is also important for performance.
            // Searches with good locality will be much faster
            // since Splay is very cheap when called repeatedly.
            Splay();

            return this.ParentNode;
        }

        // Returns the previous sibling node in logical (symbol offset) order.
        // If this node is the first node in its local binary tree, returns
        // null.
        //
        // This method will splay the returned node up to local root.
        internal SplayTreeNode GetPreviousNode()
        {
            SplayTreeNode walkerNode;
            SplayTreeNode previousNode;
            SplayTreeNodeRole role;

            previousNode = this.LeftChildNode;

            if (previousNode != null)
            {
                // Return the max node of the left child.
                while (true)
                {
                    walkerNode = previousNode.RightChildNode;
                    if (walkerNode == null)
                        break;
                    previousNode = walkerNode;
                }
            }
            else
            {
                // No left child, walk up the tree.
                role = this.Role;
                previousNode = this.ParentNode;
                while (true)
                {
                    if (role == SplayTreeNodeRole.LocalRoot)
                    {
                        previousNode = null;
                        break;
                    }
                    if (role == SplayTreeNodeRole.RightChild)
                        break;

                    role = previousNode.Role;
                    previousNode = previousNode.ParentNode;
                }
            }

            if (previousNode != null)
            {
                // Splay to keep the tree balanced.
                previousNode.Splay();
            }

            return previousNode;
        }

        // Returns the next sibling node in logical (symbol offset) order.
        // If this node is the last node in its local binary tree, returns
        // null.
        //
        // This method will splay the returned node up to local root.
        internal SplayTreeNode GetNextNode()
        {
            SplayTreeNode walkerNode;
            SplayTreeNode nextNode;
            SplayTreeNodeRole role;

            nextNode = this.RightChildNode;

            if (nextNode != null)
            {
                // Return the min node of the right child.
                while (true)
                {
                    walkerNode = nextNode.LeftChildNode;
                    if (walkerNode == null)
                        break;
                    nextNode = walkerNode;
                }
            }
            else
            {
                // No right child, walk up the tree.
                role = this.Role;
                nextNode = this.ParentNode;
                while (true)
                {
                    if (role == SplayTreeNodeRole.LocalRoot)
                    {
                        nextNode = null;
                        break;
                    }
                    if (role == SplayTreeNodeRole.LeftChild)
                        break;

                    role = nextNode.Role;
                    nextNode = nextNode.ParentNode;
                }
            }

            if (nextNode != null)
            {
                // Splay to keep the tree balanced.
                nextNode.Splay();
            }

            return nextNode;
        }

        // Returns the symbol offset of a node.
        // In the worst case this takes log time, but we use a cache that's
        // good for a tree generation.
        internal int GetSymbolOffset(uint treeGeneration)
        {
            SplayTreeNode node;
            int offset;

            offset = 0;
            node = this;

            // Each iteration walks a sibling tree.
            while (true)
            {
                // We can early out if we have a cache hit.
                // We'll always get a cache hit on the root node, if not earlier.
                if (node.Generation == treeGeneration &&
                    node.SymbolOffsetCache >= 0)
                {
                    offset += node.SymbolOffsetCache;
                    break;
                }

                // Splay this node up to the root, we're referencing it.
                node.Splay();

                // Offset gets everything to the left of this node.
                offset += node.LeftSymbolCount;
                // Add the parent start edge.
                offset += 1;

                node = node.ParentNode;
            }

            // Update this node's generation and cache.
            this.Generation = treeGeneration;
            this.SymbolOffsetCache = offset;
            
            return offset;
        }

        // Returns the character offset of a node.
        // In the worst case this takes log time.
        internal int GetIMECharOffset()
        {
            SplayTreeNode node;
            int charOffset;

            charOffset = 0;
            node = this;

            // Each iteration walks a sibling tree.
            while (true)
            {
                // Splay this node up to the root, we're referencing it.
                node.Splay();

                // Offset gets everything to the left of this node.
                charOffset += node.LeftCharCount;

                node = node.ParentNode;
                if (node == null)
                    break;

                // Add the parent start edge.
                TextTreeTextElementNode elementNode = node as TextTreeTextElementNode;
                if (elementNode != null)
                {
                    charOffset += elementNode.IMELeftEdgeCharCount;
                }
            }

            return charOffset;
        }

        // Inserts a node at a specified position.
        internal void InsertAtNode(SplayTreeNode positionNode, ElementEdge edge)
        {
            SplayTreeNode locationNode;
            bool insertBefore;

            if (edge == ElementEdge.BeforeStart || edge == ElementEdge.AfterEnd)
            {
                // Insert to this node's tree.
                InsertAtNode(positionNode, edge == ElementEdge.BeforeStart /* insertBefore */);
            }
            else
            {
                // Insert to this node's contained tree.

                if (edge == ElementEdge.AfterStart)
                {
                    locationNode = positionNode.GetFirstContainedNode();
                    insertBefore = true;
                }
                else // ElementEdge == BeforeEnd
                {
                    locationNode = positionNode.GetLastContainedNode();
                    insertBefore = false;
                }

                if (locationNode == null)
                {
                    // Inserting the first contained node.
                    positionNode.ContainedNode = this;
                    this.ParentNode = positionNode;
                    Invariant.Assert(this.LeftChildNode == null);
                    Invariant.Assert(this.RightChildNode == null);
                    Invariant.Assert(this.LeftSymbolCount == 0);
                }
                else
                {
                    InsertAtNode(locationNode, insertBefore);
                }
            }
        }

        // Inserts a node before or after an existing node.
        // The new node becomes the local root.
        internal void InsertAtNode(SplayTreeNode location, bool insertBefore)
        {
            SplayTreeNode leftSubTree;
            SplayTreeNode rightSubTree;
            SplayTreeNode containingNode;

            Invariant.Assert(this.ParentNode == null, "Can't insert child node!");
            Invariant.Assert(this.LeftChildNode == null, "Can't insert node with left children!");
            Invariant.Assert(this.RightChildNode == null, "Can't insert node with right children!");

            leftSubTree = insertBefore ? location.GetPreviousNode() : location;
            if (leftSubTree != null)
            {
                rightSubTree = leftSubTree.Split();
                containingNode = leftSubTree.ParentNode;
            }
            else
            {
                rightSubTree = location;

                location.Splay();
                Invariant.Assert(location.Role == SplayTreeNodeRole.LocalRoot, "location should be local root!");
                containingNode = location.ParentNode;
            }

            // Merge everything into a new tree.
            Join(this, leftSubTree, rightSubTree);

            // Hook up the new tree to the containing node.
            this.ParentNode = containingNode;
            if (containingNode != null)
            {
                containingNode.ContainedNode = this;
            }
        }

        // Removes this node from its tree.
        internal void Remove()
        {
            SplayTreeNode containerNode;
            SplayTreeNode root;
            SplayTreeNode leftSubTree;
            SplayTreeNode rightSubTree;

            Splay();
            Invariant.Assert(this.Role == SplayTreeNodeRole.LocalRoot);

            containerNode = this.ParentNode;
            leftSubTree = this.LeftChildNode;
            rightSubTree = this.RightChildNode;

            if (leftSubTree != null)
            {
                leftSubTree.ParentNode = null;
            }
            if (rightSubTree != null)
            {
                rightSubTree.ParentNode = null;
            }

            root = Join(leftSubTree, rightSubTree);
            if (containerNode != null)
            {
                containerNode.ContainedNode = root;
            }
            if (root != null)
            {
                root.ParentNode = containerNode;
            }

            this.ParentNode = null;
            this.LeftChildNode = null;
            this.RightChildNode = null;
        }

        // Parents leftSubTree and rightSubTree to root.  Either leftSubTree and/or
        // rightSubTree may be null.
        internal static void Join(SplayTreeNode root, SplayTreeNode leftSubTree, SplayTreeNode rightSubTree)
        {
            root.LeftChildNode = leftSubTree;
            root.RightChildNode = rightSubTree;

            Invariant.Assert(root.Role == SplayTreeNodeRole.LocalRoot);

            if (leftSubTree != null)
            {
                leftSubTree.ParentNode = root;
                root.LeftSymbolCount = leftSubTree.LeftSymbolCount + leftSubTree.SymbolCount;
                root.LeftCharCount = leftSubTree.LeftCharCount + leftSubTree.IMECharCount;
            }
            else
            {
                root.LeftSymbolCount = 0;
                root.LeftCharCount = 0;
            }

            if (rightSubTree != null)
            {
                rightSubTree.ParentNode = root;
            }
        }

        // Combines two trees.  Every node in leftSubTree will precede every node
        // in rightSubTree.
        // leftSubTree and/or rightSubTree may be null, returns null if both
        // trees are null.
        internal static SplayTreeNode Join(SplayTreeNode leftSubTree, SplayTreeNode rightSubTree)
        {
            SplayTreeNode maxNode;

            Invariant.Assert(leftSubTree == null || leftSubTree.ParentNode == null);
            Invariant.Assert(rightSubTree == null || rightSubTree.ParentNode == null);

            if (leftSubTree != null)
            {
                // Get max of leftSubTree, and splay it.
                maxNode = leftSubTree.GetMaxSibling();

                maxNode.Splay();
                Invariant.Assert(maxNode.Role == SplayTreeNodeRole.LocalRoot);
                Invariant.Assert(maxNode.RightChildNode == null);

                // Then merge the two trees.
                // No change to any LeftSymbolCounts.
                maxNode.RightChildNode = rightSubTree;
                if (rightSubTree != null)
                {
                    rightSubTree.ParentNode = maxNode;
                }
            }
            else if (rightSubTree != null)
            {
                maxNode = rightSubTree;
                Invariant.Assert(maxNode.Role == SplayTreeNodeRole.LocalRoot);
            }
            else
            {
                maxNode = null;
            }

            return maxNode;
        }

        // Splits the node's tree into two trees.  This node becomes the root of
        // a tree containing itself and all nodes preceding.  The return value
        // is a tree of all remaining nodes, the nodes that follow this node.
        internal SplayTreeNode Split()
        {
            SplayTreeNode rightSubTree;

            Splay();
            Invariant.Assert(this.Role == SplayTreeNodeRole.LocalRoot, "location should be local root!");

            rightSubTree = this.RightChildNode;
            if (rightSubTree != null)
            {
                rightSubTree.ParentNode = null;
                this.RightChildNode = null;
            }

            return rightSubTree;
        }

        // Returns the node with lowest symbol offset rooted by this node.
        internal SplayTreeNode GetMinSibling()
        {
            SplayTreeNode node;
            SplayTreeNode leftChildNode;

            node = this;

            while (true)
            {
                leftChildNode = node.LeftChildNode;
                if (leftChildNode == null)
                    break;
                node = leftChildNode;
            }

            // Splay to keep the tree balanced.
            node.Splay();

            return node;
        }

        // Returns the node with highest symbol offset rooted by this node.
        internal SplayTreeNode GetMaxSibling()
        {
            SplayTreeNode node;
            SplayTreeNode rightChildNode;

            node = this;

            while (true)
            {
                rightChildNode = node.RightChildNode;
                if (rightChildNode == null)
                    break;
                node = rightChildNode;
            }

            // Splay to keep the tree balanced.
            node.Splay();

            return node;
        }

        // Rotates this node to the top of its tree -- on exit this node will be
        // a tree root.
        //
        // Splaying is more than just a convenience function.  It is the
        // mechanism we use to keep our sibling trees balanced.  On each access
        // of a node (after moving a TextPointer to a node, or finding a
        // node by symbol offset, etc.) we Splay the node.  With random access
        // this keeps the tree balanced with a maximum depth logrithmic
        // to its size.  On sequential access, we get even better performance --
        // constant time overhead in the best case.
        //
        // Many algorithm books and the internet have descriptions of the splay
        // tree algorithm.  This is a standard implementation, nothing fancy.
        internal void Splay()
        {
            SplayTreeNode node;
            SplayTreeNode parentNode;
            SplayTreeNode grandParentNode;
            SplayTreeNodeRole nodeRole;
            SplayTreeNodeRole parentNodeRole;

            node = this;

            while (true)
            {
                nodeRole = node.Role;

                // Stop when node is the local root.
                if (nodeRole == SplayTreeNodeRole.LocalRoot)
                    break;

                parentNode = node.ParentNode;
                parentNodeRole = parentNode.Role;

                if (parentNodeRole == SplayTreeNodeRole.LocalRoot)
                {
                    // ZIG: Parent is the local root.
                    //
                    //      |             |         
                    //      Y             X         
                    //     / \           / \        
                    //    X   c   ==>   a   Y  
                    //   / \               / \    
                    //  a   b             b   c  
                    //
                    if (nodeRole == SplayTreeNodeRole.LeftChild)
                    {
                        parentNode.RotateRight();
                    }
                    else
                    {
                        parentNode.RotateLeft();
                    }
                    break;
                }
                else
                {
                    grandParentNode = parentNode.ParentNode;

                    if (nodeRole == parentNodeRole)
                    {
                        // ZIG-ZIG: node and parent are both left/right children.
                        //
                        //        |             |
                        //        Z             X
                        //       / \           / \
                        //      Y   d         a   Y
                        //     / \      ==>      / \
                        //    X   c             b   Z
                        //   / \                   / \
                        //  a   b                 c   d
                        //
                        if (nodeRole == SplayTreeNodeRole.LeftChild)
                        {
                            grandParentNode.RotateRight();
                            parentNode.RotateRight();
                        }
                        else
                        {
                            grandParentNode.RotateLeft();
                            parentNode.RotateLeft();
                        }
                    }
                    else
                    {
                        // ZIG-ZAG: node is left/right child and parent is right/left child.
                        //
                        //      |               |
                        //      Z               X
                        //     / \            /   \
                        //    Y   d          Y     Z
                        //   / \      ==>   / \   / \
                        //  a   X          a   b c   d 
                        //     / \
                        //    b   c
                        //
                        if (nodeRole == SplayTreeNodeRole.LeftChild)
                        {
                            parentNode.RotateRight();
                            grandParentNode.RotateLeft();
                        }
                        else
                        {
                            parentNode.RotateLeft();
                            grandParentNode.RotateRight();
                        }
                    }
                }
            }

            Invariant.Assert(this.Role == SplayTreeNodeRole.LocalRoot, "Splay didn't move node to root!");
        }

        // Returns true if this node is the left/right/contained node of parentNode.
        internal bool IsChildOfNode(SplayTreeNode parentNode)
        {
            return (parentNode.LeftChildNode == this ||
                    parentNode.RightChildNode == this ||
                    parentNode.ContainedNode == this);
        }

#if DEBUG
        // Allocates a unique debug-only identifier for a node.
        internal static int GetDebugId()
        {
            return _debugIdCounter++;
        }
#endif // DEBUG

        #endregion Internal methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        // If this node is a local root, then ParentNode contains it.
        // Otherwise, this is the node parenting this node within its tree.
        internal abstract SplayTreeNode ParentNode { get; set; }

        // Root node of a contained tree, if any.
        internal abstract SplayTreeNode ContainedNode { get; set; }

        // Left child node in a sibling tree.
        internal abstract SplayTreeNode LeftChildNode { get; set; }

        // Right child node in a sibling tree.
        internal abstract SplayTreeNode RightChildNode { get; set; }

        // Count of symbols covered by this node and any contained nodes.
        internal abstract int SymbolCount { get; set; }

        // Count of unicode chars covered by this node and any contained nodes.
        internal abstract int IMECharCount { get; set; }

        // Count of symbols of all siblings preceding this node.
        internal abstract int LeftSymbolCount { get; set; }

        // Count of unicode chars of all siblings preceding this node.
        internal abstract int LeftCharCount { get; set; }

        // The TextContainer's generation when SymbolOffsetCache was last updated.
        // If the current generation doesn't match TextContainer.Generation, then
        // SymbolOffsetCache is invalid.
        internal abstract uint Generation { get; set; }

        // Cached symbol offset.
        internal abstract int SymbolOffsetCache { get; set; }

        // The relative position of this node in its sibling tree.
        internal SplayTreeNodeRole Role
        {
            get
            {
                SplayTreeNode parentNode;
                SplayTreeNodeRole role;

                parentNode = this.ParentNode;

                if (parentNode == null || parentNode.ContainedNode == this)
                {
                    role = SplayTreeNodeRole.LocalRoot;
                }
                else if (parentNode.LeftChildNode == this)
                {
                    role = SplayTreeNodeRole.LeftChild;
                }
                else
                {
                    Invariant.Assert(parentNode.RightChildNode == this, "Node has no relation to parent!");
                    role = SplayTreeNodeRole.RightChild;
                }

                return role;
            }
        }

#if DEBUG
        // Debug-only identifier for this node.
        internal int DebugId
        {
            get
            {
                return _debugId;
            }
        }
#endif // DEBUG

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        // Moves this node up one level in its tree, while preserving the
        // relative order of all other nodes in the tree.
        //
        // this == X below:
        //
        //      |              |
        //      X              Y
        //     / \            / \
        //    a   Y    ==>   X   c
        //       / \        / \
        //      b   c      a   b
        //
        private void RotateLeft()
        {
            SplayTreeNode parentNode;
            SplayTreeNode rightChildNode;
            SplayTreeNode rightChildNodeChild;

            Invariant.Assert(this.RightChildNode != null, "Can't rotate left with null right child!");

            rightChildNode = this.RightChildNode;
            this.RightChildNode = rightChildNode.LeftChildNode;
            if (rightChildNode.LeftChildNode != null)
            {
                rightChildNode.LeftChildNode.ParentNode = this;
            }

            parentNode = this.ParentNode;
            rightChildNode.ParentNode = parentNode;

            if (parentNode == null)
            {
                // rightChildNode is the new local root.
                // But the local root isn't parented.
            }
            else if (parentNode.ContainedNode == this)
            {
                // rightChildNode is the new local root.
                parentNode.ContainedNode = rightChildNode;
            }
            else
            {
                // rightChildNode is not local root.
                if (this.Role == SplayTreeNodeRole.LeftChild)
                {
                    parentNode.LeftChildNode = rightChildNode;
                }
                else
                {
                    parentNode.RightChildNode = rightChildNode;
                }
            }

            rightChildNode.LeftChildNode = this;
            this.ParentNode = rightChildNode;

            // Fix rightChildNode.LeftChildNode (which has now moved to be node's LeftChildNode).
            rightChildNodeChild = this.RightChildNode;

            // Fix up the LeftSymbolCount for rightChildNode.
            // This node's LeftSymbolCount hasn't changed.
            rightChildNode.LeftSymbolCount += this.LeftSymbolCount + this.SymbolCount;
            rightChildNode.LeftCharCount += this.LeftCharCount + this.IMECharCount;
        }

        // Moves this node up one level in its tree, while preserving the
        // relative order of all other nodes in the tree.
        //
        // this == Y below:
        //
        //      |             |         
        //      Y             X         
        //     / \           / \        
        //    X   c   ==>   a   Y  
        //   / \               / \    
        //  a   b             b   c  
        //
        private void RotateRight()
        {
            SplayTreeNode parentNode;
            SplayTreeNode leftChildNode;
            SplayTreeNode leftChildNodeChild;

            Invariant.Assert(this.LeftChildNode != null, "Can't rotate right with null left child!");

            leftChildNode = this.LeftChildNode;
            this.LeftChildNode = leftChildNode.RightChildNode;
            if (leftChildNode.RightChildNode != null)
            {
                leftChildNode.RightChildNode.ParentNode = this;
            }

            parentNode = this.ParentNode;
            leftChildNode.ParentNode = parentNode;

            if (parentNode == null)
            {
                // leftChildNode is the new local root.
                // But the local root isn't parented.
            }
            else if (parentNode.ContainedNode == this)
            {
                // leftChildNode is the new local root.
                parentNode.ContainedNode = leftChildNode;
            }
            else
            {
                // leftChildNode is not local root.
                if (this.Role == SplayTreeNodeRole.LeftChild)
                {
                    parentNode.LeftChildNode = leftChildNode;
                }
                else
                {
                    parentNode.RightChildNode = leftChildNode;
                }
            }

            leftChildNode.RightChildNode = this;
            this.ParentNode = leftChildNode;

            // Fix leftChildNode.RightChildNode (which has now moved to be node's LeftChildNode).
            leftChildNodeChild = this.LeftChildNode;

            // Fix up the LeftSymbolCount for node.
            // leftChildNode's LeftSymbolCount hasn't changed.
            this.LeftSymbolCount -= leftChildNode.LeftSymbolCount + leftChildNode.SymbolCount;
            this.LeftCharCount -= leftChildNode.LeftCharCount + leftChildNode.IMECharCount;
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

#if DEBUG
        // A debug-only identifier for this node.
        private int _debugId = GetDebugId();

        // Debug-only counter for allocating debug ids.
        private static int _debugIdCounter;
#endif // DEBUG

        #endregion Private Fields
    }
}
