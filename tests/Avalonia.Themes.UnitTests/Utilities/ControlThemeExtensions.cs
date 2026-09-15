using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace Avalonia.Themes.UnitTests.Utilities;

internal static class ControlThemeExtensions
{
    public static ITemplateResult? ResolveTemplateResult(this ControlTheme theme)
    {
        var template = theme.ResolveTemplate();

        if (template is IControlTemplate controlTemplate)
        {
            var control = new TemplatedControl();
            return controlTemplate.Build(control);
        }

        if (template is IWindowDrawnDecorationsTemplate windowDrawnDecorationsTemplate)
        {
            return windowDrawnDecorationsTemplate.Build();
        }

        return null;
    }

    public static IEnumerable<AvaloniaObject> EnumerateTemplateChildren(this StyleBase styleBase)
    {
        var targetType = ResolveTargetType(styleBase) ??
                         throw new ArgumentException("Style must have TargetType set", nameof(styleBase));

        return styleBase.ResolveTemplate() switch
        {
            IControlTemplate controlTemplate => EnumerateControlTemplateChildren(targetType, controlTemplate),
            IWindowDrawnDecorationsTemplate decorationsTemplate => EnumerateDecorationsChildren(decorationsTemplate),
            _ => []
        };
    }

    private static IEnumerable<AvaloniaObject> EnumerateControlTemplateChildren(
        Type targetType, IControlTemplate controlTemplate)
    {
        TemplatedControl templatedParent;
        try
        {
            templatedParent = (TemplatedControl)Activator.CreateInstance(targetType)!;
            templatedParent.Template = controlTemplate;
            templatedParent.ApplyTemplate();
        }
        catch
        {
            templatedParent = new TemplatedControl();
            templatedParent.Template = controlTemplate;
            templatedParent.ApplyTemplate();
        }

        var templateRoot = (Control)templatedParent.GetVisualChildren().First();

        // Use visual tree instead of logical tree for the most complete result.
        // Because logical tree might not necessary 1to1 map to XAML.
        // And we filter by the same TemplateParent anyway.
        return templateRoot.GetVisualDescendants().Where(v => v.TemplatedParent == templatedParent).Prepend(templateRoot);
    }

    private static IEnumerable<AvaloniaObject> EnumerateDecorationsChildren(IWindowDrawnDecorationsTemplate template)
    {
        var decorations = new WindowDrawnDecorations { Template = template };
        decorations.ApplyTemplate();

        if (decorations.Content is not { } content)
        {
            return [];
        }

        // Window decorations have unique template configuration, that we need to resolve for each part:
        return new[] { content.Underlay, content.Overlay, content.FullscreenPopover }
            .OfType<Control>()
            .SelectMany(root => root.GetVisualDescendants().Prepend(root))
            .Where(v => v.TemplatedParent == decorations);
    }

    public static object? ResolveTemplate(this StyleBase styleBase)
    {
        var targetType = ResolveTargetType(styleBase) ??
                         throw new ArgumentException("Style must have TargetType set", nameof(styleBase));

        object? template = null;

        if (styleBase.Setters.OfType<Setter>()
                .FirstOrDefault(s => s.Property == TemplatedControl.TemplateProperty
                                     || s.Property == WindowDrawnDecorations.TemplateProperty)?
                .Value is { } setterValue)
        {
            if (setterValue is IControlTemplate or IWindowDrawnDecorationsTemplate)
            {
                template = setterValue;
            }
        }

        // ContentControl derived controls inherit its template.
        if (template is null && typeof(ContentControl).IsAssignableFrom(targetType))
        {
            template = TemplatedControl.TemplateProperty.GetDefaultValue(typeof(ContentControl))
                       ?? throw new InvalidOperationException("ContentControl must always have default template");
        }

        return template;
    }

    private static Type? ResolveTargetType(StyleBase styleBase)
    {
        if (styleBase is ControlTheme controlTheme)
        {
            return controlTheme.TargetType;
        }

        if (styleBase is Style style)
        {
            if (style.Selector?.TargetType is { } selectorTargetType)
            {
                return selectorTargetType;
            }

            if (style.Parent is StyleBase styleParent)
            {
                return ResolveTargetType(styleParent);
            }
        }

        if (styleBase is ContainerQuery { Parent: StyleBase containerQueryParent })
        {
            return ResolveTargetType(containerQueryParent);
        }

        return null;
    }
}
