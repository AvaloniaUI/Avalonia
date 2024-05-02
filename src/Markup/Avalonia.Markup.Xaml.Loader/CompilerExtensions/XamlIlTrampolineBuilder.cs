using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using XamlX.Emit;
using XamlX.IL;
using XamlX.TypeSystem;
using Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers;
using System.Reflection.Emit;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions
{
    internal class XamlIlTrampolineBuilder
    {
        private IXamlTypeBuilder<IXamlILEmitter> _builder;
        private Dictionary<string, IXamlMethod> _trampolines = new();

        public XamlIlTrampolineBuilder(IXamlTypeBuilder<IXamlILEmitter> builder)
        {
            _builder = builder;
        }

        public IXamlMethod EmitCommandExecuteTrampoline(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlMethod executeMethod)
        {
            Debug.Assert(!executeMethod.IsStatic);
            string methodName = $"{executeMethod.DeclaringType.GetFqn()}+{executeMethod.Name}_{executeMethod.Parameters.Count}!CommandExecuteTrampoline";
            if (_trampolines.TryGetValue(methodName, out var method))
            {
                return method;
            }
            var trampoline = _builder.DefineMethod(
                context.Configuration.WellKnownTypes.Void,
                new[] { context.Configuration.WellKnownTypes.Object, context.Configuration.WellKnownTypes.Object },
                methodName,
                XamlVisibility.Public,
                true,
                false);
            var gen = trampoline.Generator;
            if (executeMethod.DeclaringType.IsValueType)
            {
                gen.Ldarg_0()
                    .Unbox(executeMethod.DeclaringType);
            }
            else
            {
                gen.Ldarg_0()
                    .Castclass(executeMethod.DeclaringType);
            }
            if (executeMethod.Parameters.Count != 0)
            {
                Debug.Assert(executeMethod.Parameters.Count == 1);
                if (executeMethod.Parameters[0] != context.Configuration.WellKnownTypes.Object)
                {
                    var convertedValue = gen.DefineLocal(context.Configuration.WellKnownTypes.Object);
                    gen.Ldtype(executeMethod.Parameters[0])
                        .Ldarg(1)
                        .EmitCall(context.Configuration.WellKnownTypes.CultureInfo.FindMethod(m => m.Name == "get_CurrentCulture"))
                        .Ldloca(convertedValue)
                        .EmitCall(
                            context.GetAvaloniaTypes().TypeUtilities.FindMethod(m => m.Name == "TryConvert"),
                            swallowResult: true)
                        .Ldloc(convertedValue)
                        .Unbox_Any(executeMethod.Parameters[0]);
                }
                else
                {
                    gen.Ldarg(1);
                }
            }
            gen.EmitCall(executeMethod, swallowResult: true);
            gen.Ret();

            _trampolines.Add(methodName, trampoline);
            return trampoline;
        }

        public IXamlMethod EmitCommandCanExecuteTrampoline(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlMethod canExecuteMethod)
        {
            Debug.Assert(!canExecuteMethod.IsStatic);
            Debug.Assert(canExecuteMethod.Parameters.Count == 1);
            Debug.Assert(canExecuteMethod.ReturnType == context.Configuration.WellKnownTypes.Boolean);
            string methodName = $"{canExecuteMethod.DeclaringType.GetFqn()}+{canExecuteMethod.Name}!CommandCanExecuteTrampoline";
            if (_trampolines.TryGetValue(methodName, out var method))
            {
                return method;
            }
            var trampoline = _builder.DefineMethod(
                context.Configuration.WellKnownTypes.Boolean,
                new[] { context.Configuration.WellKnownTypes.Object, context.Configuration.WellKnownTypes.Object },
                methodName,
                XamlVisibility.Public,
                true,
                false);
            if (canExecuteMethod.DeclaringType.IsValueType)
            {
                trampoline.Generator
                    .Ldarg_0()
                    .Unbox(canExecuteMethod.DeclaringType);
            }
            else
            {
                trampoline.Generator
                    .Ldarg_0()
                    .Castclass(canExecuteMethod.DeclaringType);
            }
            trampoline.Generator
                .Ldarg(1)
                .Emit(OpCodes.Tailcall)
                .EmitCall(canExecuteMethod)
                .Ret();

            _trampolines.Add(methodName, trampoline);
            return trampoline;
        }
    }
}
