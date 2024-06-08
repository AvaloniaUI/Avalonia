// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Tmds.DBus.Protocol;

namespace Tmds.DBus.CodeGen
{
    internal class DBusObjectProxyTypeBuilder
    {
        private static readonly ConstructorInfo s_messageWriterConstructor = typeof(MessageWriter).GetConstructor(Type.EmptyTypes);
        private static readonly Type[] s_dbusObjectProxyConstructorParameterTypes = new Type[] { typeof(Connection2), typeof(IProxyFactory), typeof(string), typeof(ObjectPath2) };
        private static readonly ConstructorInfo s_baseConstructor = typeof(DBusObjectProxy).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, s_dbusObjectProxyConstructorParameterTypes);
        private static readonly ConstructorInfo s_signatureConstructor = typeof(Signature).GetConstructor(new Type[] { typeof(string) });
        private static readonly ConstructorInfo s_nullableSignatureConstructor = typeof(Signature?).GetConstructor(new Type[] { typeof(Signature) });
        private static readonly MethodInfo s_callNonVoidMethod = typeof(DBusObjectProxy).GetMethod(nameof(DBusObjectProxy.CallNonVoidMethodAsync), BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo s_callGenericOutMethod = typeof(DBusObjectProxy).GetMethod(nameof(DBusObjectProxy.CallGenericOutMethodAsync), BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo s_callVoidMethod = typeof(DBusObjectProxy).GetMethod(nameof(DBusObjectProxy.CallVoidMethodAsync), BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo s_watchNonVoidSignal = typeof(DBusObjectProxy).GetMethod(nameof(DBusObjectProxy.WatchNonVoidSignalAsync), BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo s_watchVoidSignal = typeof(DBusObjectProxy).GetMethod(nameof(DBusObjectProxy.WatchVoidSignalAsync), BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly Type s_nullableSignatureType = typeof(Signature?);
        private static readonly Type s_dbusObjectProxyType = typeof(DBusObjectProxy);
        private static readonly Type s_messageWriterType = typeof(MessageWriter);
        private static readonly Type s_readMethodDelegateGenericType = typeof(ReadMethodDelegate<>);

        private readonly ModuleBuilder _moduleBuilder;
        private TypeBuilder _typeBuilder;

        public DBusObjectProxyTypeBuilder(ModuleBuilder module)
        {
            _moduleBuilder = module;
        }

        public TypeInfo Build(Type interfaceType)
        {
            if (_typeBuilder != null)
            {
                throw new InvalidOperationException("Type has already been built.");
            }

            var proxyName = interfaceType.FullName + "Proxy";
            _typeBuilder = _moduleBuilder.DefineType(proxyName, TypeAttributes.Class | TypeAttributes.Public, s_dbusObjectProxyType);

            var description = TypeDescription.DescribeInterface(interfaceType);

            ImplementConstructor();

            if (!description.Interfaces.Any(dbusInterface => dbusInterface.Type == interfaceType))
            {
                _typeBuilder.AddInterfaceImplementation(interfaceType);
            }

            foreach (var dbusInterface in description.Interfaces)
            {
                ImplementDBusInterface(dbusInterface);
            }

            return _typeBuilder.CreateTypeInfo();
        }

        private void ImplementConstructor()
        {
            //DBusConnection connection, IProxyFactory factory, string serviceName, ObjectPath objectPath
            var constructor = _typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, s_dbusObjectProxyConstructorParameterTypes);

            ILGenerator ilg = constructor.GetILGenerator();

            // base constructor
            ilg.Emit(OpCodes.Ldarg_0);
            ilg.Emit(OpCodes.Ldarg_1);
            ilg.Emit(OpCodes.Ldarg_2);
            ilg.Emit(OpCodes.Ldarg_3);
            ilg.Emit(OpCodes.Ldarg, 4);
            ilg.Emit(OpCodes.Call, s_baseConstructor);

            ilg.Emit(OpCodes.Ret);
        }

        private void ImplementDBusInterface(InterfaceDescription dbusInterface)
        {
            _typeBuilder.AddInterfaceImplementation(dbusInterface.Type);

            foreach (var method in dbusInterface.Methods)
            {
                ImplementMethod(method, false);
            }

            foreach (var method in new[] { dbusInterface.GetAllPropertiesMethod,
                                           dbusInterface.GetPropertyMethod,
                                           dbusInterface.SetPropertyMethod
                                         })
            {
                if (method == null)
                {
                    continue;
                }
                ImplementMethod(method, true);
            }

            foreach (var signal in dbusInterface.Signals)
            {
                ImplementSignal(signal, false);
            }

            if (dbusInterface.PropertiesChangedSignal != null)
            {
                ImplementSignal(dbusInterface.PropertiesChangedSignal, true);
            }
        }

        private void ImplementSignal(SignalDescription signalDescription, bool isPropertiesChanged)
        {
            var method = _typeBuilder.ImplementInterfaceMethod(signalDescription.MethodInfo);
            ILGenerator ilg = method.GetILGenerator();

            // call Watch(...)

            // BusObject (this)
            ilg.Emit(OpCodes.Ldarg_0);
            ilg.Emit(OpCodes.Castclass, s_dbusObjectProxyType);

            // Interface
            ilg.Emit(OpCodes.Ldstr, signalDescription.Interface.Name);

            // Member
            ilg.Emit(OpCodes.Ldstr, signalDescription.Name);

            // Action<Exception>
            if (signalDescription.HasOnError)
            {
                ilg.Emit(OpCodes.Ldarg_2);
            }
            else
            {
                ilg.Emit(OpCodes.Ldnull);
            }

            // Action/Action<>
            ilg.Emit(OpCodes.Ldarg_1);

            if (signalDescription.SignalType != null)
            {
                // ReadMethodDelegate
                ilg.Emit(OpCodes.Ldnull);
                ilg.Emit(OpCodes.Ldftn, ReadMethodFactory.CreateReadMethodForType(signalDescription.SignalType));
                var readDelegateConstructor = s_readMethodDelegateGenericType.MakeGenericType(new[] { signalDescription.SignalType }).GetConstructors()[0];
                ilg.Emit(OpCodes.Newobj, readDelegateConstructor);

                // isPropertiesChanged
                ilg.Emit(isPropertiesChanged ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);

                // Watch
                ilg.Emit(OpCodes.Call, s_watchNonVoidSignal.MakeGenericMethod(new[] { signalDescription.SignalType }));
            }
            else
            {
                // Watch
                ilg.Emit(OpCodes.Call, s_watchVoidSignal);
            }

            ilg.Emit(OpCodes.Ret);
        }

        private void ImplementMethod(MethodDescription methodDescription, bool propertyMethod)
        {
            var method = _typeBuilder.ImplementInterfaceMethod(methodDescription.MethodInfo);

            ILGenerator ilg = method.GetILGenerator();

            //CallMethod(...)

            // BusObject (this)
            ilg.Emit(OpCodes.Ldarg_0);
            ilg.Emit(OpCodes.Castclass, s_dbusObjectProxyType);

            // Interface
            if (propertyMethod)
            {
                ilg.Emit(OpCodes.Ldstr, "org.freedesktop.DBus.Properties");
            }
            else
            {
                ilg.Emit(OpCodes.Ldstr, methodDescription.Interface.Name);
            }

            // Member
            ilg.Emit(OpCodes.Ldstr, methodDescription.Name);

            // Signature
            if (methodDescription.InSignature.HasValue || propertyMethod)
            {
                string inSig = methodDescription.InSignature?.Value ?? string.Empty;
                if (propertyMethod)
                {
                    inSig = "s" + inSig;
                }
                ilg.Emit(OpCodes.Ldstr, inSig);
                ilg.Emit(OpCodes.Newobj, s_signatureConstructor);
                ilg.Emit(OpCodes.Newobj, s_nullableSignatureConstructor);
            }
            else
            {
                LocalBuilder signature = ilg.DeclareLocal(s_nullableSignatureType);
                ilg.Emit(OpCodes.Ldloca_S, signature);
                ilg.Emit(OpCodes.Initobj, s_nullableSignatureType);
                ilg.Emit(OpCodes.Ldloc, signature);
            }

            // MessageWriter
            var argumentOffset = 1; //offset by one to account for "this"
            if (methodDescription.InArguments.Count != 0 || propertyMethod)
            {
                LocalBuilder writer = ilg.DeclareLocal(s_messageWriterType);
                ilg.Emit(OpCodes.Newobj, s_messageWriterConstructor);
                ilg.Emit(OpCodes.Stloc, writer);

                if (propertyMethod)
                {
                    // Write parameter
                    Type parameterType = typeof(string);
                    ilg.Emit(OpCodes.Ldloc, writer);
                    ilg.Emit(OpCodes.Ldstr, methodDescription.Interface.Name);
                    ilg.Emit(OpCodes.Call, WriteMethodFactory.CreateWriteMethodForType(parameterType, isCompileTimeType: true));
                }

                foreach (var argument in methodDescription.InArguments)
                {
                    // Write parameter
                    Type parameterType = argument.Type;
                    ilg.Emit(OpCodes.Ldloc, writer);
                    ilg.Emit(OpCodes.Ldarg, argumentOffset);
                    ilg.Emit(OpCodes.Call, WriteMethodFactory.CreateWriteMethodForType(parameterType, isCompileTimeType: true));

                    argumentOffset++;
                }

                ilg.Emit(OpCodes.Ldloc, writer);
            }
            else
            {
                ilg.Emit(OpCodes.Ldnull);
            }

            if (methodDescription.OutType != null)
            {
                // CallMethod
                if (methodDescription.IsGenericOut)
                {
                    Type genericParameter = method.GetGenericArguments()[0];
                    ilg.Emit(OpCodes.Call, s_callGenericOutMethod.MakeGenericMethod(new[] { genericParameter }));
                }
                else
                {
                    // ReadMethodDelegate
                    ilg.Emit(OpCodes.Ldnull);
                    ilg.Emit(OpCodes.Ldftn, ReadMethodFactory.CreateReadMethodForType(methodDescription.OutType));
                    var readDelegateConstructor = s_readMethodDelegateGenericType.MakeGenericType(new[] { methodDescription.OutType }).GetConstructors()[0];
                    ilg.Emit(OpCodes.Newobj, readDelegateConstructor);

                    ilg.Emit(OpCodes.Call, s_callNonVoidMethod.MakeGenericMethod(new[] { methodDescription.OutType }));
                }
            }
            else
            {
                // CallMethod
                ilg.Emit(OpCodes.Call, s_callVoidMethod);
            }

            ilg.Emit(OpCodes.Ret);
        }
    }
}
