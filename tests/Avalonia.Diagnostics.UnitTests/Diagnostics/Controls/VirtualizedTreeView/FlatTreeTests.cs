using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Avalonia.Diagnostics.Controls.VirtualizedTreeView;
using Xunit;

namespace Avalonia.Diagnostics.UnitTests;

public class FlatTreeTests
{
    private class MockTreeNode : ITreeNode
    {
        private bool _isExpanded;

        public MockTreeNode(params MockTreeNode[] children)
        {
            foreach (var child in children)
            {
                ChildrenNodes.Add(child);
            }

            ChildrenNodes.CollectionChanged += (_, e) =>
            {
                CollectionChanged?.Invoke(this, e);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasChildren)));
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public ObservableCollection<MockTreeNode> ChildrenNodes { get; } = new();
        public IReadOnlyList<ITreeNode> Children => ChildrenNodes;
        public bool HasChildren => ChildrenNodes.Count > 0;
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                _isExpanded = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpanded)));
            }
        }
    }

    [Fact]
    public void FlatTree_Empty_When_No_Roots_On_Create()
    {
        var roots = Array.Empty<MockTreeNode>();
        var flatTree = new FlatTree(roots);

        AssertFlatTreeEqual(roots, flatTree);
    }

    [Fact]
    public void FlatTree_Contains_Only_Roots_When_All_Collapsed_On_Create()
    {
        var roots = new MockTreeNode[]
        {
            new (new MockTreeNode()),
            new (new MockTreeNode()),
            new (new MockTreeNode()),
            new (new MockTreeNode())
        };
        var flatTree = new FlatTree(roots);

        AssertFlatTreeEqual(roots, flatTree);
    }

    [Fact]
    public void FlatTree_Contains_All_Nodes_When_All_Expanded_On_Create()
    {
        var roots = new MockTreeNode[]
        {
            new (new MockTreeNode(new MockTreeNode(), new MockTreeNode())),
            new (new MockTreeNode()),
            new (new MockTreeNode(new MockTreeNode())),
            new (new MockTreeNode())
        };
        ExpandAll(roots);
        var flatTree = new FlatTree(roots);

        AssertFlatTreeEqual(roots, flatTree);
    }

    [Fact]
    public void FlatTree_Displays_Children_When_Node_Becomes_Expanded()
    {
        var roots = new MockTreeNode[]
        {
            new (new MockTreeNode(new MockTreeNode(), new MockTreeNode())),
        };
        var flatTree = new FlatTree(roots);

        roots[0].IsExpanded = true;

        AssertFlatTreeEqual(roots, flatTree);
    }

    [Fact]
    public void FlatTree_Ignores_Double_Property_Changes()
    {
        var roots = new MockTreeNode[]
        {
            new (new MockTreeNode(new MockTreeNode(), new MockTreeNode())),
        };
        var flatTree = new FlatTree(roots);

        roots[0].IsExpanded = true;
        roots[0].IsExpanded = true;

        AssertFlatTreeEqual(roots, flatTree);

        roots[0].IsExpanded = false;
        roots[0].IsExpanded = false;

        AssertFlatTreeEqual(roots, flatTree);
    }

    [Fact]
    public void FlatTree_Hides_Children_When_Node_Becomes_Collapsed()
    {
        var roots = new MockTreeNode[]
        {
            new (new MockTreeNode(new MockTreeNode(), new MockTreeNode())),
        };
        ExpandAll(roots);
        var flatTree = new FlatTree(roots);

        roots[0].IsExpanded = false;

        AssertFlatTreeEqual(roots, flatTree);
    }

    [Fact]
    public void FlatTree_Ignores_Expand_When_Collapsed_Node_Is_Expanded()
    {
        var roots = new MockTreeNode[]
        {
            new (new MockTreeNode(new MockTreeNode(), new MockTreeNode())),
        };
        var flatTree = new FlatTree(roots);

        roots[0].ChildrenNodes[0].IsExpanded = false;

        AssertFlatTreeEqual(roots, flatTree);
    }

    [Fact]
    public void FlatTree_Adds_Children_When_One_But_Last_Root_Expanded()
    {
        var roots = new MockTreeNode[]
        {
            new (new MockTreeNode()),
            new (new MockTreeNode()),
            new (new MockTreeNode(), new MockTreeNode()),
            new (new MockTreeNode()),
        };
        var flatTree = new FlatTree(roots);

        ExpandAll(new []{roots[2]});

        AssertFlatTreeEqual(roots, flatTree);
    }

    [Fact]
    public void FlatTree_Lazily_Adds_Children_For_Infinite_Trees()
    {
        MockTreeNode Create()
        {
            var node = new MockTreeNode();
            node.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName != nameof(MockTreeNode.IsExpanded))
                    return;

                if (node.IsExpanded)
                    node.ChildrenNodes.Add(Create());
            };
            return node;
        }
        var roots = new MockTreeNode[]
        {
            Create(),
            Create(),
            Create(),
            Create(),
        };
        var flatTree = new FlatTree(roots);

        roots[0].IsExpanded = true;

        AssertFlatTreeEqual(roots, flatTree);

        roots[0].ChildrenNodes[0].IsExpanded = true;

        AssertFlatTreeEqual(roots, flatTree);
    }

    [Fact]
    public void FlatTree_Dynamically_Adds_Child_To_Empty_Node()
    {
        var roots = new MockTreeNode[]
        {
            new () { IsExpanded = true }
        };
        var flatTree = new FlatTree(roots);

        roots[0].ChildrenNodes.Add(new MockTreeNode());

        AssertFlatTreeEqual(roots, flatTree);
    }

    [Fact]
    public void FlatTree_Dynamically_Adds_Child_To_Non_Empty_Node()
    {
        var roots = new MockTreeNode[]
        {
            new (new MockTreeNode()) { IsExpanded = true },
            new (new MockTreeNode(), new MockTreeNode(), new MockTreeNode()) { IsExpanded = true },
            new (new MockTreeNode()) { IsExpanded = true },
        };
        var flatTree = new FlatTree(roots);

        roots[1].ChildrenNodes.Insert(1, new MockTreeNode());
        roots[1].ChildrenNodes.Insert(2,
            new MockTreeNode(new MockTreeNode()) { IsExpanded = true });

        AssertFlatTreeEqual(roots, flatTree);
    }

    [Fact]
    public void FlatTree_Ignores_Collapsed_Children_Removal()
    {
        var roots = new MockTreeNode[]
        {
            new (new MockTreeNode(), new MockTreeNode()),
        };
        var flatTree = new FlatTree(roots);

        roots[0].ChildrenNodes.RemoveAt(0);
        roots[0].ChildrenNodes.RemoveAt(0);

        AssertFlatTreeEqual(roots, flatTree);
    }

    [Fact]
    public void FlatTree_Hides_Node_On_Children_Removal()
    {
        var roots = new MockTreeNode[]
        {
            new (new MockTreeNode(), new MockTreeNode()) { IsExpanded = true },
        };
        var flatTree = new FlatTree(roots);

        roots[0].ChildrenNodes.RemoveAt(1);
        roots[0].ChildrenNodes.RemoveAt(0);

        AssertFlatTreeEqual(roots, flatTree);
    }

    [Fact]
    public void FlatTree_Hides_Nodes_Recursively_On_Children_Removal()
    {
        var roots = new MockTreeNode[]
        {
            new (new MockTreeNode(), new MockTreeNode()),
            new (new MockTreeNode(), new MockTreeNode(
                new MockTreeNode(new MockTreeNode(), new MockTreeNode()), new MockTreeNode())),
            new (new MockTreeNode(), new MockTreeNode())
        };
        ExpandAll(roots);
        var flatTree = new FlatTree(roots);

        roots[0].ChildrenNodes.RemoveAt(1);

        AssertFlatTreeEqual(roots, flatTree);
    }

    [Fact]
    public void FlatTree_Nodes_Can_Be_Expanded_After_Being_Expanded_And_Collapsed()
    {
        var roots = new MockTreeNode[]
        {
            new (new MockTreeNode(), new MockTreeNode(
                new MockTreeNode(new MockTreeNode(), new MockTreeNode()), new MockTreeNode()))
        };
        var flatTree = new FlatTree(roots);

        ExpandAll(roots);
        CollapseAll(roots);
        ExpandAll(roots);

        AssertFlatTreeEqual(roots, flatTree);
    }

    private void AssertFlatTreeEqual(MockTreeNode[] roots, FlatTree flatTree)
    {
        void FlattenExpandedNodes(ITreeNode node, List<ITreeNode> output)
        {
            output.Add(node);
            if (node.IsExpanded)
            {
                foreach (var child in node.Children)
                {
                    FlattenExpandedNodes(child, output);
                }
            }
        }

        List<ITreeNode> FlattenExpandedTree()
        {
            List<ITreeNode> output = new();
            foreach (var root in roots)
            {
                FlattenExpandedNodes(root, output);
            }
            return output;
        }

        var expected = FlattenExpandedTree();
        var actual = flatTree.Select(x => x.Node).ToList();

        Assert.Equal(expected, actual);
    }

    private void ExpandAll(MockTreeNode[] roots)
    {
        Traverse(roots, node => node.IsExpanded = true);
    }

    private void CollapseAll(MockTreeNode[] roots)
    {
        Traverse(roots, node => node.IsExpanded = false);
    }

    private void Traverse(MockTreeNode[] roots, Action<MockTreeNode> action)
    {
        void Recur(MockTreeNode node)
        {
            action(node);
            foreach (var child in node.ChildrenNodes)
            {
                Recur(child);
            }
        }

        foreach (var root in roots)
        {
            Recur(root);
        }
    }
}
