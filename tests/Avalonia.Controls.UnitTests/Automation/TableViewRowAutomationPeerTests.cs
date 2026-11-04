using System.Linq;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests.Automation;

public sealed class TableViewRowAutomationPeerTests : ScopedTestBase
{
    [Fact]
    public void Row_Name_Comes_From_Cell_Values_And_Selection_Is_Preserved()
    {
        using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);
        var target = CreateTarget();
        target.Columns.Add(new TableViewColumn { Binding = new Binding("Name") });
        target.Columns.Add(new TableViewColumn { Binding = new Binding("Severity") });
        target.ItemsSource = new[] { new Row("boot.log", "Warning") };
        Prepare(target);

        var row = (TableViewRow)target.GetRealizedContainers().Single();
        var peer = ControlAutomationPeer.CreatePeerForElement(row);
        var selectionItem = Assert.IsAssignableFrom<ISelectionItemProvider>(
            peer.GetProvider<ISelectionItemProvider>());

        Assert.Equal("boot.log Warning", peer.GetName());
        Assert.DoesNotContain(nameof(Row), peer.GetName()!);
        Assert.Equal(AutomationControlType.DataItem, peer.GetAutomationControlType());

        selectionItem.Select();
        Assert.True(row.IsSelected);
    }

    [Fact]
    public void Templated_Cell_Contributes_All_Visible_Text()
    {
        using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);
        var target = CreateTarget();
        target.Columns.Add(new TableViewColumn
        {
            CellTemplate = new FuncDataTemplate<Row>((_, _) => new StackPanel
            {
                Children =
                {
                    new TextBlock { [!TextBlock.TextProperty] = new Binding("Name") },
                    new TextBlock { [!TextBlock.TextProperty] = new Binding("Severity") },
                },
            }),
        });
        target.ItemsSource = new[] { new Row("boot.log", "Warning") };
        Prepare(target);

        var row = (TableViewRow)target.GetRealizedContainers().Single();

        Assert.Equal("boot.log Warning", ControlAutomationPeer.CreatePeerForElement(row).GetName());
    }

    [Fact]
    public void Explicit_Row_Automation_Name_Wins()
    {
        using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);
        var target = CreateTarget();
        target.Columns.Add(new TableViewColumn { Binding = new Binding("Name") });
        target.ItemsSource = new[] { new Row("boot.log", "Warning") };
        Prepare(target);

        var row = (TableViewRow)target.GetRealizedContainers().Single();
        AutomationProperties.SetName(row, "Explicit row name");

        Assert.Equal("Explicit row name", ControlAutomationPeer.CreatePeerForElement(row).GetName());
    }

    [Fact]
    public void Explicit_Cell_Automation_Name_Is_Used()
    {
        using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);
        var target = CreateTarget();
        target.Columns.Add(new TableViewColumn
        {
            CellTemplate = new FuncDataTemplate<Row>((_, _) =>
            {
                var text = new TextBlock { Text = "Visible text" };
                AutomationProperties.SetName(text, "Accessible cell name");
                return text;
            }),
        });
        target.ItemsSource = new[] { new Row("boot.log", "Warning") };
        Prepare(target);

        var row = (TableViewRow)target.GetRealizedContainers().Single();

        Assert.Equal("Accessible cell name", ControlAutomationPeer.CreatePeerForElement(row).GetName());
    }

    [Fact]
    public void Hidden_And_Raw_Cell_Content_Is_Excluded()
    {
        using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);
        var target = CreateTarget();
        target.Columns.Add(new TableViewColumn { Binding = new Binding("Name") });
        target.Columns.Add(new TableViewColumn { Binding = new Binding("Severity"), IsVisible = false });
        target.Columns.Add(new TableViewColumn
        {
            CellTemplate = new FuncDataTemplate<Row>((_, _) =>
            {
                var panel = new StackPanel
                {
                    Children =
                    {
                        new TextBlock { Text = "Visible status" },
                        new TextBlock { Text = "Decorative" },
                    },
                };
                AutomationProperties.SetAccessibilityView(panel.Children[1], AccessibilityView.Raw);
                return panel;
            }),
        });
        target.ItemsSource = new[] { new Row("boot.log", "Hidden severity") };
        Prepare(target);

        var row = (TableViewRow)target.GetRealizedContainers().Single();

        Assert.Equal("boot.log Visible status", ControlAutomationPeer.CreatePeerForElement(row).GetName());
    }

    [Fact]
    public void Raw_Cell_With_An_Explicit_Automation_Name_Is_Excluded()
    {
        using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);
        var target = CreateTarget();
        target.Columns.Add(new TableViewColumn { Binding = new Binding("Name") });
        target.Columns.Add(new TableViewColumn { Binding = new Binding("Severity") });
        target.ItemsSource = new[] { new Row("boot.log", "Warning") };
        Prepare(target);

        var row = (TableViewRow)target.GetRealizedContainers().Single();
        var rawCell = Assert.IsType<TableViewCell>(row.CellsPresenter!.Children[1]);
        AutomationProperties.SetAccessibilityView(rawCell, AccessibilityView.Raw);
        AutomationProperties.SetName(rawCell, "Raw cell name");

        Assert.Equal("boot.log", ControlAutomationPeer.CreatePeerForElement(row).GetName());
    }

    [Fact]
    public void Raw_Cell_With_Ordinary_Content_Is_Excluded()
    {
        using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);
        var target = CreateTarget();
        target.Columns.Add(new TableViewColumn { Binding = new Binding("Name") });
        target.Columns.Add(new TableViewColumn { Binding = new Binding("Severity") });
        target.ItemsSource = new[] { new Row("boot.log", "Warning") };
        Prepare(target);

        var row = (TableViewRow)target.GetRealizedContainers().Single();
        var rawCell = Assert.IsType<TableViewCell>(row.CellsPresenter!.Children[1]);
        AutomationProperties.SetAccessibilityView(rawCell, AccessibilityView.Raw);

        Assert.Equal("boot.log", ControlAutomationPeer.CreatePeerForElement(row).GetName());
    }

    [Fact]
    public void A_Row_Without_Realized_Cells_Uses_The_Inherited_Name()
    {
        using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);
        var row = new TableViewRow { Content = "plain" };

        Assert.Equal("plain", ControlAutomationPeer.CreatePeerForElement(row).GetName());
    }

    private static TableView CreateTarget() => new()
    {
        Template = new FuncControlTemplate<TableView>((_, scope) =>
            new ItemsPresenter { Name = "PART_ItemsPresenter" }.RegisterInNameScope(scope)),
        ItemContainerTheme = new ControlTheme(typeof(TableViewRow))
        {
            Setters =
            {
                new Setter(
                    TemplatedControl.TemplateProperty,
                    new FuncControlTemplate<TableViewRow>((_, scope) =>
                        new TableViewCellsPresenter { Name = "PART_CellsPresenter" }.RegisterInNameScope(scope))),
            },
        },
    };

    private static void Prepare(TableView target)
    {
        target.Width = 300;
        target.Height = 200;
        var root = new TestRoot(target);
        root.LayoutManager.ExecuteInitialLayoutPass();
    }

    private sealed class Row(string name, string severity)
    {
        public string Name { get; } = name;

        public string Severity { get; } = severity;
    }
}
