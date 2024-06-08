// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace Tmds.DBus.CodeGen
{
    internal static  class TypeBuilderExtensions
    {
        private static readonly Type s_parameterTaskType = typeof(Task<>);

        public static MethodBuilder OverrideAbstractMethod(this TypeBuilder typeBuilder, MethodInfo declMethod)
        {
            var attributes = declMethod.Attributes;
            attributes ^= MethodAttributes.NewSlot;
            attributes |= MethodAttributes.ReuseSlot;
            attributes |= MethodAttributes.Final;

            return DefineMethod(typeBuilder, attributes, declMethod);
        }

        public static MethodBuilder ImplementInterfaceMethod(this TypeBuilder typeBuilder, MethodInfo declMethod)
        {
            var attributes = declMethod.Attributes;
            attributes ^= MethodAttributes.Abstract;
            attributes ^= MethodAttributes.NewSlot;
            attributes |= MethodAttributes.Final;

            return DefineMethod(typeBuilder, attributes, declMethod);
        }

        private static MethodBuilder DefineMethod(TypeBuilder typeBuilder, MethodAttributes attributes, MethodInfo declMethod)
        {
            if (declMethod.IsGenericMethod)
            {
                return DefineGenericMethod(typeBuilder, attributes, declMethod);
            }
            var declParameters = declMethod.GetParameters();

            var defineParameters = new Type[declParameters.Length];
            for (var i = 0; i < declParameters.Length; i++)
                defineParameters[i] = declParameters[i].ParameterType;

            var methodBuilder = typeBuilder.DefineMethod(declMethod.Name, attributes, declMethod.ReturnType, defineParameters);
            typeBuilder.DefineMethodOverride(methodBuilder, declMethod);

            for (var i = 0; i < declParameters.Length; i++)
                methodBuilder.DefineParameter(i + 1, declParameters[i].Attributes, declParameters[i].Name);

            return methodBuilder;
        }

        private static MethodBuilder DefineGenericMethod(TypeBuilder typeBuilder, MethodAttributes attributes, MethodInfo declMethod)
        {
            var declParameters = declMethod.GetParameters();

            var defineParameters = new Type[declParameters.Length];
            for (var i = 0; i < declParameters.Length; i++)
                defineParameters[i] = declParameters[i].ParameterType;

            var methodBuilder = typeBuilder.DefineMethod(declMethod.Name, attributes); //, declMethod.ReturnType, defineParameters);
            GenericTypeParameterBuilder[] typeParameters = methodBuilder.DefineGenericParameters(new[] { "T" });
            methodBuilder.SetParameters(defineParameters);
            methodBuilder.SetReturnType(s_parameterTaskType.MakeGenericType(new Type[] { typeParameters[0] }));

            typeBuilder.DefineMethodOverride(methodBuilder, declMethod);

            for (var i = 0; i < declParameters.Length; i++)
                methodBuilder.DefineParameter(i + 1, declParameters[i].Attributes, declParameters[i].Name);

            return methodBuilder;
        }
    }
}
