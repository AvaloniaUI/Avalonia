using System.Collections.Generic;
using System.Linq;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers;

internal class AvaloniaXamlIlTransformRoutedEvent : IXamlAstTransformer
{
    public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
    {
        if (node is XamlAstNamePropertyReference prop
            && prop.TargetType is XamlAstClrTypeReference targetRef
            && prop.DeclaringType is XamlAstClrTypeReference declaringRef)
        {
            var xkt = context.GetAvaloniaTypes();
            var interactiveType = xkt.Interactivity.Interactive;
            var routedEventType = xkt.Interactivity.RoutedEvent;
            var AddHandlerT = xkt.Interactivity.AddHandlerT;

            if (interactiveType.IsAssignableFrom(targetRef.Type))
            {
                var eventName = $"{prop.Name}Event";
                if (declaringRef.Type.GetAllFields().FirstOrDefault(f => f.IsStatic && f.Name == eventName) is { } eventField)
                {
                    if (routedEventType.IsAssignableFrom(eventField.FieldType))
                    {
                        var instance = new XamlAstClrProperty(prop
                            , prop.Name
                            , targetRef.Type
                            , null
                            );
                        instance.Setters.Add(new XamlDirectCallAddHandler(eventField,
                            targetRef.Type,
                            xkt.Interactivity.AddHandler,
                            xkt.Interactivity.RoutedEventHandler
                            )
                        );
                        if (eventField.FieldType.GenericArguments?.Count == 1)
                        {
                            var agrument = eventField.FieldType.GenericArguments[0];
                            if (!agrument.Equals(xkt.Interactivity.RoutedEventArgs))
                            {
                                instance.Setters.Add(new XamlDirectCallAddHandler(eventField,
                                    targetRef.Type,
                                    xkt.Interactivity.AddHandlerT.MakeGenericMethod([agrument]),
                                    xkt.EventHandlerT.MakeGenericType(agrument)
                                    )
                                );
                            }
                        }
                        return instance;
                    }
                    else
                    {
                        context.ReportDiagnostic(new XamlX.XamlDiagnostic(
                            AvaloniaXamlDiagnosticCodes.TransformError,
                            XamlX.XamlDiagnosticSeverity.Error,
                            $"Event definition {prop.Name} found, but its type {eventField.FieldType.GetFqn()} is not compatible with RoutedEvent.",
                            node));
                    }
                }
            }
        }
        return node;
    }

    private sealed class XamlDirectCallAddHandler : IXamlILOptimizedEmitablePropertySetter
    {
        private readonly IXamlField _eventField;
        private readonly IXamlType _declaringType;
        private readonly IXamlMethod _addMethod;

        public XamlDirectCallAddHandler(IXamlField eventField,
            IXamlType declaringType,
            IXamlMethod addMethod,
            IXamlType routedEventHandler
            )
        {
            Parameters = [routedEventHandler];
            _eventField = eventField;
            _declaringType = declaringType;
            _addMethod = addMethod;
        }

        public IXamlType TargetType => _declaringType;
        public PropertySetterBinderParameters BinderParameters { get; } = new PropertySetterBinderParameters();
        public IReadOnlyList<IXamlType> Parameters { get; }

        public IReadOnlyList<IXamlCustomAttribute> CustomAttributes => [];

        public void Emit(IXamlILEmitter emitter)
            => emitter.EmitCall(_addMethod, true);

        public void EmitWithArguments(XamlEmitContextWithLocals<IXamlILEmitter, XamlILNodeEmitResult> context,
            IXamlILEmitter emitter,
            IReadOnlyList<IXamlAstValueNode> arguments)
        {

            using (var loc = emitter.LocalsPool.GetLocal(_declaringType))
                emitter
                    .Ldloc(loc.Local);

            emitter.Ldfld(_eventField);

            for (var i = 0; i < arguments.Count; ++i)
                context.Emit(arguments[i], emitter, Parameters[i]);

            emitter.Ldc_I4(5);
            emitter.Ldc_I4(0);

            emitter.EmitCall(_addMethod, true);
        }
    }
}
