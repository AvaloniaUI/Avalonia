using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers;
using XamlX;
using XamlX.Ast;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers;

#if !XAMLX_INTERNAL
public
#endif
    class AvaloniaXamlIlDataTemplateWarningsTransformer : IXamlAstTransformer
{
    public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
    {
        var avaloniaTypes = context.GetAvaloniaTypes();
        var contentControl = context.GetAvaloniaTypes().ContentControl;

        // This transformers only looks for ContentControl delivered objects inside of DataTemplate
        if ((node is not XamlAstObjectNode objectNode)
            || !contentControl.IsAssignableFrom(objectNode.Type.GetClrType())
            || context.ParentNodes().FirstOrDefault() is not XamlAstObjectNode parentNode
            || !avaloniaTypes.IDataTemplate.IsAssignableFrom(parentNode.Type.GetClrType()))
        {
            return node;
        }

        // And only inside of ItemTemplate or DataTemplates property value.
        if (context.ParentNodes().OfType<XamlAstXamlPropertyValueNode>().FirstOrDefault() is not { } valueNode
            || valueNode.Property.GetClrProperty() is not { } clrProperty
            || !((clrProperty.Name == "ItemTemplate" && clrProperty.DeclaringType == avaloniaTypes.ItemsControl)
                || (clrProperty.Name == "DataTemplates" && clrProperty.DeclaringType == avaloniaTypes.Control)))
        {
            return node;
        }

        // And only inside of ItemsControl
        if (context.ParentNodes().SkipWhile(p => p != valueNode)
                .OfType<XamlAstObjectNode>().FirstOrDefault() is not { } itemsControlNode
            || !avaloniaTypes.ItemsControl.IsAssignableFrom(itemsControlNode.Type.GetClrType()))
        {
            return node;
        }

        // Avalonia doesn't have any reliable way to determine container type from the API.
        if (GetKnownItemContainerTypeFullName(itemsControlNode.Type.GetClrType()) is not { } knownItemContainerTypeName
            || itemsControlNode.Type.GetClrType().Assembly?.FindType(knownItemContainerTypeName) is not { } knownItemContainerType)
        {
            return node;
        }

        if (knownItemContainerType.IsAssignableFrom(objectNode.Type.GetClrType()))
        {
            context.ReportDiagnostic(new XamlDiagnostic(
                AvaloniaXamlDiagnosticCodes.ItemContainerInsideTemplate,
                XamlDiagnosticSeverity.Warning,
                $"Unexpected '{knownItemContainerType.Name}' inside of '{itemsControlNode.Type.GetClrType().Name}.{clrProperty.Name}'. "
                + $"'{itemsControlNode.Type.GetClrType().Name}.{clrProperty.Name}' defines template of the container content, not the container itself.", node));
        }

        return node;
    }

    private static string? GetKnownItemContainerTypeFullName(IXamlType itemsControlType) => itemsControlType.FullName switch
    {
        "Avalonia.Controls.ListBox" => "Avalonia.Controls.ListBoxItem",
        "Avalonia.Controls.ComboBox" => "Avalonia.Controls.ComboBoxItem",
        "Avalonia.Controls.Menu" => "Avalonia.Controls.MenuItem",
        "Avalonia.Controls.MenuItem" => "Avalonia.Controls.MenuItem",
        "Avalonia.Controls.Primitives.TabStrip" => "Avalonia.Controls.Primitives.TabStripItem",
        "Avalonia.Controls.TabControl" => "Avalonia.Controls.TabItem",
        "Avalonia.Controls.TreeView" => "Avalonia.Controls.TreeViewItem",
        _ => null
    };
}
