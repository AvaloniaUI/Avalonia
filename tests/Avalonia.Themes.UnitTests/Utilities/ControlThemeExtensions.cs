using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace Avalonia.Themes.UnitTests.Utilities;

internal static class ControlThemeExtensions
{
    public static ITemplateResult? ResolveTemplate(this ControlTheme theme)
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

        if (template is null)
        {
            return null;
        }

        return template switch
        {
            IControlTemplate controlTemplate => controlTemplate.Build(
                (TemplatedControl)RuntimeHelpers.GetUninitializedObject(theme.TargetType)),
            IWindowDrawnDecorationsTemplate windowDrawnDecorationsTemplate => windowDrawnDecorationsTemplate.Build()
        };
    }

    public static IEnumerable<AvaloniaObject> EnumerateTemplateChildren(this Control templateRoot)
    {
        var templatedParent = templateRoot.TemplatedParent;
        if (templatedParent is null)
        {
            // No templated parent - no template at all.
            return [];
        }

        // Use visual tree instead of logical tree for the most complete result.
        // Because logical tree might not necessary 1to1 map to XAML.
        // And we filter by the same TemplateParent anyway. 
        return templateRoot.GetVisualDescendants().Where(v => v.TemplatedParent == templatedParent).Prepend(templateRoot);
    }
}
