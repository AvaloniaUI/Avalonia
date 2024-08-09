using System.Collections.Generic;
using System.Linq;
using XamlX.Ast;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers;

internal class AvaloniaXamlIlEventSetterTransformer : IXamlAstTransformer
{
    public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
    {
        if (!(node is XamlAstObjectNode on
              && on.Type.GetClrType().FullName == "Avalonia.Styling.EventSetter"))
            return node;

        IXamlType targetType = null;

        var styleParent = context.ParentNodes()
            .OfType<AvaloniaXamlIlTargetTypeMetadataNode>()
            .FirstOrDefault(x => x.ScopeType == AvaloniaXamlIlTargetTypeMetadataNode.ScopeTypes.Style);

        if (styleParent != null)
        {
            targetType = styleParent.TargetType.GetClrType()
                ?? throw new XamlStyleTransformException("Can not find parent Style Selector or ControlTemplate TargetType. If setter is not part of the style, you can set x:SetterTargetType directive on its parent.", node);
        }

        if (targetType == null)
        {
            throw new XamlStyleTransformException("Could not determine target type of Setter", node);
        }

        var routedEventProp = on.Children.OfType<XamlAstXamlPropertyValueNode>()
            .FirstOrDefault(x => x.Property.GetClrProperty().Name == "Event");
        var routedEventType = context.GetAvaloniaTypes().RoutedEvent;
        var routedEventTType = context.GetAvaloniaTypes().RoutedEventT;
        IXamlType actualRoutedEventType;
        if (routedEventProp != null)
        {
            if (routedEventProp.Values.OfType<XamlStaticExtensionNode>().FirstOrDefault() is not { } valueNode)
            {
                var eventName = routedEventProp.Values.OfType<XamlAstTextNode>().FirstOrDefault()?.Text;
                if (eventName == null)
                    throw new XamlStyleTransformException("EventSetter.Event must be an event name or x:Static expression to an event.", node);

                // Ideally, compiler should do one of these lookups:
                // - Runtime lookup in RoutedEventRegistry. But can't do that in compiler. Could be the best approach, if we inject RoutedEventRegistry extra code, still no compile time alidation.
                // - Lookup for CLR events with this name and routed args. But there might not be a CLR event at all. And we can't reliably get RoutedEvent instance from there.
                // - Lookup for AddEventNameHandler methods. But the same - there might not be one.
                // Instead this transformer searches for routed event definition in one of base classes. Not ideal too, it won't handle attached events. Still better than others.
                // Combining with ability to use x:Static for uncommon use-cases this approach should work well. 
                if (!eventName.EndsWith("Event"))
                {
                    eventName += "Event";
                }

                IXamlType type = targetType, nextType = targetType;
                IXamlMember member = null;
                while (nextType is not null && member is null)
                {
                    type = nextType;
                    member = type?.Fields.FirstOrDefault(f => f.IsPublic && f.IsStatic && f.Name == eventName) ??
                             (IXamlMember)type?.GetAllProperties().FirstOrDefault(p =>
                                 p.Name == eventName && p.Getter is { IsPublic: true, IsStatic: true });
                    nextType = type.BaseType;
                }

                if (member is null)
                    throw new XamlStyleTransformException($"EventSetter.Event with name \"{eventName}\" wasn't found.", node);

                valueNode = new XamlStaticExtensionNode(on,
                    new XamlAstClrTypeReference(routedEventProp, type, false), member.Name);

                routedEventProp.Values = new List<IXamlAstValueNode> {valueNode};
            }

            actualRoutedEventType = valueNode.Type.GetClrType();
            if (!routedEventType.IsAssignableFrom(actualRoutedEventType))
                throw new XamlStyleTransformException("EventSetter.Event must be assignable to RoutedEvent type.", node);

            // Get RoutedEvent or RoutedEvent<T> base type from the field, so we can get generic parameter below.
            // This helps to ignore any other possible MyRoutedEvent : RoutedEvent<MyArgs> type definitions.
            while (!(actualRoutedEventType.FullName.StartsWith(routedEventType.FullName)
                     || actualRoutedEventType.FullName.StartsWith(routedEventTType.FullName)))
            {
                actualRoutedEventType = actualRoutedEventType.BaseType;
            }
        }
        else
        {
            throw new XamlStyleTransformException($"EventSetter.Event must be set.", node);
        }

        var handlerProp = on.Children.OfType<XamlAstXamlPropertyValueNode>()
            .FirstOrDefault(x => x.Property.GetClrProperty().Name == "Handler");
        if (handlerProp != null)
        {
            var handlerName = handlerProp.Values.OfType<XamlAstTextNode>().FirstOrDefault()?.Text;
            if (handlerName == null)
                throw new XamlStyleTransformException("EventSetter.Handler must be a method name.", node);

            var rootType = context.RootObject?.Type.GetClrType();
            var argsType = actualRoutedEventType.GenericArguments.Any() ?
                actualRoutedEventType.GenericArguments[0] :
                context.GetAvaloniaTypes().RoutedEventArgs;

            var handler = rootType?.FindMethod(
                handlerName,
                context.Configuration.WellKnownTypes.Void,
                true,
                new[] { context.Configuration.WellKnownTypes.Object, argsType });
            if (handler != null)
            {
                var delegateType = context.Configuration.TypeSystem.GetType("System.EventHandler`1").MakeGenericType(argsType);
                var handlerInvoker = new XamlLoadMethodDelegateNode(handlerProp, context.RootObject, delegateType, handler);
                handlerProp.Values = new List<IXamlAstValueNode> { handlerInvoker };
            }
            else
            {
                throw new XamlStyleTransformException(
                    $"EventSetter.Handler with name \"{handlerName}\" wasn't found on type \"{rootType?.Name ?? "(null)"}\"." +
                    $"EventSetter is supported only on XAML files with x:Class.", node);
            }
        }
        else
        {
            throw new XamlStyleTransformException($"EventSetter.Handler must be set.", node);
        }

        return on;
    }
}
