using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.PropertyStore;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.UnitTests;

public class SetterTemplateLifetimeTests : ScopedTestBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Discarding_Setter_Content_Clears_Its_TemplateBindings(bool invalidateStyles)
    {
        var target = CreateTarget();
        var style = CreateStyle(BuildContent);
        var root = new TestRoot { Styles = { style }, Child = target };
        var content = Assert.IsType<Border>(target.Content);
        var text = Assert.IsType<TextBlock>(content.Child);
        Assert.Equal("Initial", text.Text);

        if (invalidateStyles)
            target.InvalidateStyles(recurse: false);
        else
            root.Styles.Remove(style);

        Assert.Null(content.TemplatedParent);
        Assert.Null(text.TemplatedParent);
        target.Tag = "Changed";
        Assert.NotEqual("Changed", text.Text);

        if (invalidateStyles)
            target.ApplyStyling();
        else
        {
            root.Styles.Add(style);
            target.ApplyStyling();
        }

        var replacement = Assert.IsType<Border>(target.Content);
        Assert.NotSame(content, replacement);
        Assert.Equal("Changed", Assert.IsType<TextBlock>(replacement.Child).Text);
    }

    [Fact]
    public void Reactivating_Setter_Content_Restores_Its_TemplateBindings()
    {
        var target = CreateTarget();
        target.Classes.Add("active");
        var style = CreateStyle(BuildContent, activeClass: true);
        var root = new TestRoot { Styles = { style }, Child = target };
        var content = Assert.IsType<Border>(target.Content);
        var text = Assert.IsType<TextBlock>(content.Child);

        target.Classes.Remove("active");

        Assert.Null(target.Content);
        Assert.Null(content.TemplatedParent);
        Assert.Null(text.TemplatedParent);
        target.Tag = "While inactive";
        Assert.NotEqual("While inactive", text.Text);

        target.Classes.Add("active");

        Assert.Same(content, target.Content);
        Assert.Same(target, content.TemplatedParent);
        Assert.Same(target, text.TemplatedParent);
        Assert.Equal("While inactive", text.Text);
        target.Tag = "Active again";
        Assert.Equal("Active again", text.Text);
    }

    [Fact]
    public void Discarding_Setter_Content_Preserves_Nested_Template_Ownership()
    {
        var nested = new ContentControl
        {
            Tag = "Nested",
            Template = new FuncControlTemplate<ContentControl>((_, _) => BuildText()),
        };
        var target = CreateTarget();
        var root = new TestRoot
        {
            Styles = { CreateStyle(() => new Border { Child = nested }) },
            Child = target,
        };
        var content = Assert.IsType<Border>(target.Content);
        nested.ApplyTemplate();
        var nestedText = Assert.IsType<TextBlock>(Assert.Single(nested.GetVisualChildren()));
        Assert.Equal("Nested", nestedText.Text);

        target.InvalidateStyles(recurse: false);

        Assert.Null(content.TemplatedParent);
        Assert.Null(nested.TemplatedParent);
        Assert.Same(nested, nestedText.TemplatedParent);
        nested.Tag = "Nested changed";
        Assert.Equal("Nested changed", nestedText.Text);
    }

    [Fact]
    public void Discarding_Setter_Content_Clears_Owned_Children_Removed_From_The_Subtree()
    {
        var target = CreateTarget();
        var root = new TestRoot { Styles = { CreateStyle(BuildContent) }, Child = target };
        var content = Assert.IsType<Border>(target.Content);
        var text = Assert.IsType<TextBlock>(content.Child);
        content.Child = null;

        target.InvalidateStyles(recurse: false);

        Assert.Null(text.TemplatedParent);
        target.Tag = "Changed";
        Assert.NotEqual("Changed", text.Text);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Reapplying_Styles_Does_Not_Retain_Discarded_Content(bool bindingInParentTheme)
    {
        var (root, target, discarded) = CreateDiscardedContent(bindingInParentTheme);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        foreach (var reference in discarded)
            Assert.False(reference.TryGetTarget(out _));

        GC.KeepAlive(root);
        GC.KeepAlive(target);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (TestRoot, ContentControl, List<WeakReference<Control>>) CreateDiscardedContent(bool bindingInParentTheme)
    {
        var target = CreateTarget();
        if (bindingInParentTheme)
        {
            target.Theme = new ControlTheme(typeof(ContentControl))
            {
                Children =
                {
                    new Style(x => x.Nesting().Template().OfType<TextBlock>())
                    {
                        Setters = { new Setter(TextBlock.TextProperty, new TemplateBinding(Control.TagProperty)) },
                    },
                },
            };
        }
        var style = CreateStyle(() => bindingInParentTheme
            ? new Border { Child = new TextBlock() }
            : BuildContent());
        var root = new TestRoot { Styles = { style }, Child = target };
        var discarded = new List<WeakReference<Control>>();

        for (var i = 0; i < 5; ++i)
        {
            var content = Assert.IsType<Border>(target.Content);
            discarded.Add(new(content));
            discarded.Add(new(content.Child!));
            target.InvalidateStyles(recurse: false);
            target.ApplyStyling();
        }

        return (root, target, discarded);
    }

    [Fact]
    public void Null_Setter_Template_Result_Is_Materialized_Only_Once()
    {
        var builds = 0;
        var target = CreateTarget();
        var template = new FuncTemplate<Control?>(() =>
        {
            ++builds;
            return null;
        });
        var entry = (IValueEntry)new PropertySetterTemplateInstance(ContentControl.ContentProperty, template, target);

        Assert.Null(entry.GetValue());
        Assert.Null(entry.GetValue());
        entry.Unsubscribe();
        Assert.Null(entry.GetValue());

        Assert.Equal(1, builds);
    }

    [Fact]
    public void Discarding_Setter_Content_Does_Not_Clear_Reassigned_Ownership()
    {
        var target = CreateTarget();
        var root = new TestRoot { Styles = { CreateStyle(BuildContent) }, Child = target };
        var content = Assert.IsType<Border>(target.Content);
        var text = Assert.IsType<TextBlock>(content.Child);
        var newOwner = new ContentControl { Tag = "Other owner" };
        text.TemplatedParent = newOwner;

        target.InvalidateStyles(recurse: false);

        Assert.Same(newOwner, text.TemplatedParent);
        newOwner.Tag = "Other owner changed";
        Assert.Equal("Other owner changed", text.Text);
    }

    [Fact]
    public void Reading_Value_During_Teardown_Does_Not_Reattach_TemplateBindings()
    {
        var target = CreateTarget();
        var entry = (IValueEntry)new PropertySetterTemplateInstance(ContentControl.ContentProperty,
            new FuncTemplate<Control>(BuildContent), target);
        var content = Assert.IsType<Border>(entry.GetValue());
        var text = Assert.IsType<TextBlock>(content.Child);
        content.PropertyChanged += (_, e) =>
        {
            if (e.Property == StyledElement.TemplatedParentProperty && e.NewValue is null)
                Assert.Same(content, entry.GetValue());
        };

        entry.Unsubscribe();

        Assert.Null(content.TemplatedParent);
        Assert.Null(text.TemplatedParent);
        target.Tag = "Changed";
        Assert.NotEqual("Changed", text.Text);
    }

    private static ContentControl CreateTarget() => new() { Name = "target", Tag = "Initial" };

    private static Style CreateStyle(Func<Control> build, bool activeClass = false) =>
        new(x => activeClass
            ? x.OfType<ContentControl>().Name("target").Class("active")
            : x.OfType<ContentControl>().Name("target"))
        {
            Setters = { new Setter(ContentControl.ContentProperty, new FuncTemplate<Control>(build)) },
        };

    private static Control BuildContent() => new Border { Child = BuildText() };

    private static TextBlock BuildText() => new()
    {
        [!TextBlock.TextProperty] = new TemplateBinding(Control.TagProperty),
    };
}
