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

    public static IEnumerable<AvaloniaObject> EnumerateTemplateChildren(this ControlTheme theme)
    {
        if (theme.TargetType is null)
        {
            throw new ArgumentException("ControlTheme must have TargetType set", nameof(theme));
        }

        if (theme.ResolveTemplate() is not IControlTemplate controlTemplate)
        {
            return [];
        }

        TemplatedControl templatedParent;
        try
        {
            templatedParent = (TemplatedControl)Activator.CreateInstance(theme.TargetType)!;
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

    public static object? ResolveTemplate(this ControlTheme theme)
    {
        if (theme.TargetType is null)
        {
            throw new ArgumentException("ControlTheme must have TargetType set", nameof(theme));
        }

        object? template = null;

        if (theme.Setters.OfType<Setter>()
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
        if (template is null && typeof(ContentControl).IsAssignableFrom(theme.TargetType))
        {
            template = TemplatedControl.TemplateProperty.GetDefaultValue(typeof(ContentControl))
                       ?? throw new InvalidOperationException("ContentControl must always have default template");
        }

        return template;
    }
}
