using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Metadata;
using Avalonia.Styling;

[assembly: XmlnsDefinition("https://github.com/avaloniaui", "Sandbox.Controls")]
namespace Sandbox.Controls;

public class DrawnDecorationsPresenter : Control
{
    class Wrapper : Control
    {
        public Control? Inner
        {
            get=>field;
            set
            {
                if (field == value)
                    return;
                if (field != null)
                    VisualChildren.Remove(field);
                field = value;
                if (field != null)
                    VisualChildren.Add(field);
            }
        }
    }

    private WindowDrawnDecorations _decorations = new WindowDrawnDecorations();
    private Grid _grid = new Grid();
    private Wrapper _overlay = new Wrapper(), _underlay = new Wrapper();
    public DrawnDecorationsPresenter()
    {
        LogicalChildren.Add(_decorations);
        ((ISetLogicalParent)_decorations).SetParent(this);
        VisualChildren.Add(_grid);
        _grid.Children.Add(_underlay);
        _grid.Children.Add(_overlay);
    }

    public override void ApplyTemplate()
    {
        _decorations.ApplyTemplate();
        _overlay.Inner = _decorations.Content?.Overlay;
        _underlay.Inner = _decorations.Content?.Underlay;
        
        base.ApplyTemplate();
    }
}

[ControlTemplateScope]
public class WindowDrawnDecorationsTemplate : ITemplate
{
    [Content]
    [TemplateContent(TemplateResultType = typeof(WindowDrawnDecorationsContent))]
    public object? Content { get; set; }

    public TemplateResult<WindowDrawnDecorationsContent> Build() =>
        TemplateContent.Load<WindowDrawnDecorationsContent>(Content) ?? throw new InvalidOperationException();

    object? ITemplate.Build() => Build();

}

public class WindowDrawnDecorationsContent : StyledElement
{
    public Control? Overlay
    {
        get => field;
        set => HandleLogicalChild(ref field, value);
    }
    
    public Control? Underlay
    {
        get => field;
        set => HandleLogicalChild(ref field, value);
    }

    void HandleLogicalChild(ref Control? field, Control? value)
    {
        if (field == value)
            return;
        if (field != null)
        {
            LogicalChildren.Remove(field);
            ((ISetLogicalParent)field).SetParent(null);
        }

        field = value;
        if (field != null)
        {
            LogicalChildren.Add(field);
            ((ISetLogicalParent)field).SetParent(this);
        }
    }
}

[PseudoClasses(":test")]
public class WindowDrawnDecorations : StyledElement
{
    public WindowDrawnDecorations()
    {
        PseudoClasses.Add(":test");
    }
    
    public static readonly StyledProperty<WindowDrawnDecorationsTemplate> TemplateProperty = AvaloniaProperty.Register<WindowDrawnDecorations, WindowDrawnDecorationsTemplate>(
        "Template");

    public WindowDrawnDecorationsTemplate Template
    {
        get => GetValue(TemplateProperty);
        set => SetValue(TemplateProperty, value);
    }

    public static readonly StyledProperty<double> TitleBarHeightProperty = AvaloniaProperty.Register<WindowDrawnDecorations, double>(
        "TitleBarHeight");

    public double TitleBarHeight
    {
        get => GetValue(TitleBarHeightProperty);
        set => SetValue(TitleBarHeightProperty, value);
    }

    public WindowDrawnDecorationsContent? Content { get; private set; }
    
    private WindowDrawnDecorationsTemplate? _appliedTemplate;
    public void ApplyTemplate()
    {
        if (Template == _appliedTemplate)
            return;
        if (Content != null)
        {
            LogicalChildren.Remove(Content);
            ((ISetLogicalParent)Content).SetParent(null);
            Content = null;
        }

        var res = Template.Build();
        Content = res.Result;
        if (Content != null)
        {
            TemplatedControl.ApplyTemplatedParent(Content, this);
            LogicalChildren.Add(Content);
            ((ISetLogicalParent)Content).SetParent(this);
        }
    }
}

class TestTest : Border
{
    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
    }
}