using System.Collections.Generic;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.UnitTests.Automation;

public class TreeViewAutomationPeerTests : ScopedTestBase
{
    [Fact]
    public void Tree_Is_Selection_Provider()
    {
        using var app = UnitTestApplication.Start(TestServices.StyledWindow);
        var tree = CreateTree();
        var provider = GetProvider(tree);

        Assert.False(provider.CanSelectMultiple);
        Assert.False(provider.IsSelectionRequired);
        Assert.Empty(provider.GetSelection());

        tree.SelectionMode = SelectionMode.Single | SelectionMode.AlwaysSelected;

        Assert.False(provider.IsSelectionRequired);
    }

    [Fact]
    public void CanSelectMultiple_Reflects_SelectionMode()
    {
        using var app = UnitTestApplication.Start(TestServices.StyledWindow);
        var tree = CreateTree();
        tree.SelectionMode = SelectionMode.Multiple;

        Assert.True(GetProvider(tree).CanSelectMultiple);
    }

    [Fact]
    public void GetSelection_Returns_Selected_Rows()
    {
        using var app = UnitTestApplication.Start(TestServices.StyledWindow);
        var tree = CreateTree();
        var child = (TreeViewItem)((TreeViewItem)tree.Items[0]!).Items[1]!;

        tree.SelectedItem = child;

        var selected = Assert.Single(GetProvider(tree).GetSelection());
        Assert.Same(ControlAutomationPeer.CreatePeerForElement(child), selected);
    }

    [Fact]
    public void GetSelection_Returns_Visible_Selected_Rows_In_Tree_Order()
    {
        using var app = UnitTestApplication.Start(TestServices.StyledWindow);
        var hidden = new TreeViewItem { Header = "Hidden", Items = { new TreeViewItem { Header = "Hidden child" } } };
        var tree = CreateTree(hidden);
        tree.SelectionMode = SelectionMode.Multiple;
        var root = (TreeViewItem)tree.Items[0]!;
        var sibling = (TreeViewItem)tree.Items[1]!;
        var child = (TreeViewItem)root.Items[0]!;

        tree.SelectedItems.Add(sibling);
        tree.SelectedItems.Add(hidden.Items[0]);
        tree.SelectedItems.Add(child);

        var selected = GetProvider(tree).GetSelection();

        Assert.Equal(2, selected.Count);
        Assert.Same(ControlAutomationPeer.CreatePeerForElement(child), selected[0]);
        Assert.Same(ControlAutomationPeer.CreatePeerForElement(sibling), selected[1]);
    }

    [Fact]
    public void GetSelection_Excludes_Row_Under_Collapsed_Parent()
    {
        using var app = UnitTestApplication.Start(TestServices.StyledWindow);
        var tree = CreateTree();
        var parent = (TreeViewItem)tree.Items[0]!;

        tree.SelectedItem = parent.Items[0];
        parent.IsExpanded = false;

        Assert.Empty(GetProvider(tree).GetSelection());
    }

    [Fact]
    public void Collapsing_Parent_Of_Selected_Row_Raises_Selection_PropertyChanged()
    {
        using var app = UnitTestApplication.Start(TestServices.StyledWindow);
        var tree = CreateTree();
        var parent = (TreeViewItem)tree.Items[0]!;
        tree.SelectedItem = parent.Items[0];
        var raised = CollectSelectionChanges(tree);

        parent.IsExpanded = false;
        ExecuteLayoutPass(tree);

        Assert.Single(raised);
        Assert.Empty(GetProvider(tree).GetSelection());
    }

    [Fact]
    public void Expanding_Never_Opened_Parent_Of_Selected_Row_Raises_Selection_PropertyChanged()
    {
        using var app = UnitTestApplication.Start(TestServices.StyledWindow);
        var hidden = new TreeViewItem { Header = "Hidden", Items = { new TreeViewItem { Header = "Hidden child" } } };
        var tree = CreateTree(hidden);
        tree.SelectedItem = hidden.Items[0];
        var raised = CollectSelectionChanges(tree);

        Assert.Empty(GetProvider(tree).GetSelection());

        hidden.IsExpanded = true;
        ExecuteLayoutPass(tree);

        Assert.Single(raised);
        Assert.Single(GetProvider(tree).GetSelection());
    }

    [Fact]
    public void Toggling_Row_Without_Selected_Descendants_Does_Not_Raise_Selection_PropertyChanged()
    {
        using var app = UnitTestApplication.Start(TestServices.StyledWindow);
        var tree = CreateTree();
        var parent = (TreeViewItem)tree.Items[0]!;
        tree.SelectedItem = tree.Items[1];
        var raised = CollectSelectionChanges(tree);

        parent.IsExpanded = false;
        ExecuteLayoutPass(tree);
        parent.IsExpanded = true;
        ExecuteLayoutPass(tree);

        Assert.Empty(raised);
    }

    [Fact]
    public void Selection_Change_Raises_Selection_PropertyChanged()
    {
        using var app = UnitTestApplication.Start(TestServices.StyledWindow);
        var tree = CreateTree();
        var raised = CollectSelectionChanges(tree);

        tree.SelectedItem = tree.Items[1];
        tree.SelectedItem = tree.Items[0];

        Assert.Equal(2, raised.Count);
    }

    [Fact]
    public void Handled_Selection_Change_Raises_Selection_PropertyChanged()
    {
        using var app = UnitTestApplication.Start(TestServices.StyledWindow);
        var tree = CreateTree();
        tree.SelectionChanged += (_, e) => e.Handled = true;
        var raised = CollectSelectionChanges(tree);

        tree.SelectedItem = tree.Items[1];

        Assert.Single(raised);
    }

    [Fact]
    public void SelectAll_Raises_Selection_PropertyChanged_Once()
    {
        using var app = UnitTestApplication.Start(TestServices.StyledWindow);
        var tree = CreateTree();
        tree.SelectionMode = SelectionMode.Multiple;
        var raised = CollectSelectionChanges(tree);

        tree.SelectAll();

        Assert.Single(raised);
        Assert.Equal(4, GetProvider(tree).GetSelection().Count);
    }

    [Fact]
    public void Selection_Change_In_Nested_ListBox_Does_Not_Raise_Selection_PropertyChanged()
    {
        using var app = UnitTestApplication.Start(TestServices.StyledWindow);
        var listBox = new ListBox { Items = { "a", "b" } };
        var tree = CreateTree(new TreeViewItem { Header = listBox });
        var raised = CollectSelectionChanges(tree);

        listBox.SelectedIndex = 1;

        Assert.Empty(raised);
    }

    private static TreeView CreateTree(params TreeViewItem[] extraItems)
    {
        var tree = new TreeView
        {
            Items =
            {
                new TreeViewItem
                {
                    Header = "Root",
                    IsExpanded = true,
                    Items =
                    {
                        new TreeViewItem { Header = "Child 1" },
                        new TreeViewItem { Header = "Child 2" },
                    }
                },
                new TreeViewItem { Header = "Sibling" },
            }
        };

        foreach (var item in extraItems)
            tree.Items.Add(item);

        var root = new TestRoot
        {
            Resources =
            {
                [typeof(TreeView)] = new ControlTheme(typeof(TreeView))
                {
                    Setters =
                    {
                        new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<TreeView>((_, scope) =>
                            new ItemsPresenter { Name = "PART_ItemsPresenter" }.RegisterInNameScope(scope))),
                    }
                },
                [typeof(TreeViewItem)] = new ControlTheme(typeof(TreeViewItem))
                {
                    Setters =
                    {
                        new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<TreeViewItem>((parent, scope) =>
                            new StackPanel
                            {
                                Children =
                                {
                                    new ContentPresenter
                                    {
                                        Name = "PART_HeaderPresenter",
                                        [~ContentPresenter.ContentProperty] = parent[~TreeViewItem.HeaderProperty],
                                    }.RegisterInNameScope(scope),
                                    new ItemsPresenter
                                    {
                                        Name = "PART_ItemsPresenter",
                                        [~Visual.IsVisibleProperty] = parent[~TreeViewItem.IsExpandedProperty],
                                    }.RegisterInNameScope(scope),
                                }
                            })),
                    }
                },
            },
            Child = tree,
        };

        root.LayoutManager.ExecuteInitialLayoutPass();
        return tree;
    }

    private static void ExecuteLayoutPass(TreeView tree)
    {
        tree.GetLayoutManager()!.ExecuteLayoutPass();
    }

    private static ISelectionProvider GetProvider(TreeView tree)
    {
        var provider = ControlAutomationPeer.CreatePeerForElement(tree).GetProvider<ISelectionProvider>();
        Assert.NotNull(provider);
        return provider;
    }

    private static List<AutomationPropertyChangedEventArgs> CollectSelectionChanges(TreeView tree)
    {
        var peer = ControlAutomationPeer.CreatePeerForElement(tree);
        var result = new List<AutomationPropertyChangedEventArgs>();

        peer.PropertyChanged += (_, e) =>
        {
            if (e.Property == SelectionPatternIdentifiers.SelectionProperty)
                result.Add(e);
        };

        return result;
    }
}
