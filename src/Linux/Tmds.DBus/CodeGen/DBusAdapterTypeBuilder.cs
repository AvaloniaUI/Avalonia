// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;

namespace Tmds.DBus.CodeGen
{
    internal class DBusAdapterTypeBuilder
    {
        private static readonly ConstructorInfo s_messageWriterConstructor = typeof(MessageWriter).GetConstructor(Type.EmptyTypes);
        private static readonly Type[] s_dbusAdaptorConstructorParameterTypes = new Type[] { typeof(DBusConnection), typeof(ObjectPath2), typeof(object), typeof(IProxyFactory), typeof(SynchronizationContext) };
        private static readonly ConstructorInfo s_baseConstructor = typeof(DBusAdapter).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, s_dbusAdaptorConstructorParameterTypes);
        private static readonly ConstructorInfo s_methodHandlerConstructor = typeof(DBusAdapter.MethodCallHandler).GetConstructors()[0];
        private static readonly ConstructorInfo s_signatureConstructor = typeof(Signature).GetConstructor(new Type[] { typeof(string) });
        private static readonly ConstructorInfo s_nullableSignatureConstructor = typeof(Signature?).GetConstructor(new Type[] { typeof(Signature) });
        private static readonly ConstructorInfo s_messageReaderConstructor = typeof(MessageReader).GetConstructor(new Type[] { typeof(Message), typeof(IProxyFactory) });
        private static readonly FieldInfo s_methodDictionaryField = typeof(DBusAdapter).GetField(nameof(DBusAdapter._methodHandlers), BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo s_objectField = typeof(DBusAdapter).GetField(nameof(DBusAdapter._object), BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo s_methodDictionaryAdd = typeof(Dictionary<string, DBusAdapter.MethodCallHandler>).GetMethod("Add", new Type[] { typeof(string), typeof(DBusAdapter.MethodCallHandler) });
        private static readonly MethodInfo s_startWatchingSignals = typeof(DBusAdapter).GetMethod(nameof(DBusAdapter.StartWatchingSignals), BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo s_emitVoidSignal = typeof(DBusAdapter).GetMethod(nameof(DBusAdapter.EmitVoidSignal), BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo s_emitNonVoidSignal = typeof(DBusAdapter).GetMethod(nameof(DBusAdapter.EmitNonVoidSignal), BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo s_createNonVoidReply = typeof(DBusAdapter).GetMethod(nameof(DBusAdapter.CreateNonVoidReply), BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo s_createVoidReply = typeof(DBusAdapter).GetMethod(nameof(DBusAdapter.CreateVoidReply), BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo s_readerSkipString = typeof(MessageReader).GetMethod(nameof(MessageReader.SkipString), BindingFlags.Instance | BindingFlags.Public);
        private static readonly MethodInfo s_writerWriteString = typeof(MessageWriter).GetMethod(nameof(MessageWriter.WriteString), BindingFlags.Instance | BindingFlags.Public);
        private static readonly MethodInfo s_writerSetSkipNextStructPadding = typeof(MessageWriter).GetMethod(nameof(MessageWriter.SetSkipNextStructPadding), BindingFlags.Instance | BindingFlags.Public);
        private static readonly FieldInfo s_setTypeIntrospectionField = typeof(DBusAdapter).GetField(nameof(DBusAdapter._typeIntrospection), BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly Type s_taskOfMessageType = typeof(Task<Message>);
        private static readonly Type s_nullableSignatureType = typeof(Signature?);
        private static readonly Type s_action2GenericType = typeof(Action<,>);
        private static readonly Type s_messageWriterType = typeof(MessageWriter);
        private static readonly Type s_messageReaderType = typeof(MessageReader);
        // private static readonly Type s_stringType = typeof(string);
        private static readonly Type[] s_methodHandlerParameterTypes = new[] { typeof(object), typeof(Message), typeof(IProxyFactory) };

        private readonly ModuleBuilder _moduleBuilder;
        private TypeBuilder _typeBuilder;

        public DBusAdapterTypeBuilder(ModuleBuilder moduleBuilder)
        {
            _moduleBuilder = moduleBuilder;
        }

        public TypeInfo Build(Type objectType)
        {
            if (_typeBuilder != null)
            {
                throw new InvalidOperationException("Type has already been built.");
            }

            var parentType = typeof(DBusAdapter);
            var adapterName = objectType.FullName + "Adapter";
            _typeBuilder = _moduleBuilder.DefineType(adapterName, TypeAttributes.Class | TypeAttributes.Public, parentType);

            var description = TypeDescription.DescribeObject(objectType);

            ImplementConstructor(description);
            ImplementStartWatchingSignals(description.Interfaces);

            return _typeBuilder.CreateTypeInfo();
        }

        private void ImplementStartWatchingSignals(IList<InterfaceDescription> interfaces)
        {
            var signalCount = interfaces.Aggregate(0, (count, iface) => count +
                                                                        (iface.Signals?.Count ?? 0) +
                                                                        ((iface.PropertiesChangedSignal != null) ? 1 : 0));
            if (signalCount == 0)
            {
                return;
            }

            var method = _typeBuilder.OverrideAbstractMethod(s_startWatchingSignals);
            var ilg = method.GetILGenerator();

            // signals = new Task<IDisposable>[signalCount];
            ilg.Emit(OpCodes.Ldc_I4, signalCount);
            ilg.Emit(OpCodes.Newarr, typeof(Task<IDisposable>));

            var idx = 0;
            foreach (var dbusInterface in interfaces)
            {
                IEnumerable<SignalDescription> signals = dbusInterface.Signals ?? Array.Empty<SignalDescription>();

                if (dbusInterface.PropertiesChangedSignal != null)
                {
                    signals = signals.Concat(new[] { dbusInterface.PropertiesChangedSignal });
                }

                foreach (var signal in signals)
                {
                    // signals[i] = Watch((IDbusInterface)this, SendSignalAction)
                    ilg.Emit(OpCodes.Dup);          // signals
                    ilg.Emit(OpCodes.Ldc_I4, idx);  // i

                    {
                        // Watch(...)
                        {
                            // (IDbusInterface)this
                            ilg.Emit(OpCodes.Ldarg_0);
                            ilg.Emit(OpCodes.Ldfld, s_objectField);
                            ilg.Emit(OpCodes.Castclass, dbusInterface.Type);
                        }

                        {
                            //SendSignalAction
                            ilg.Emit(OpCodes.Ldarg_0);
                            ilg.Emit(OpCodes.Ldftn, GenSendSignal(signal, signal == dbusInterface.PropertiesChangedSignal));
                            ilg.Emit(OpCodes.Newobj, signal.ActionType.GetConstructors()[0]);
                        }

                        if (signal.HasOnError)
                        {
                            // Action<Exception> = null
                            ilg.Emit(OpCodes.Ldnull);
                        }

                        ilg.Emit(OpCodes.Callvirt, signal.MethodInfo);
                    }

                    ilg.Emit(OpCodes.Stelem_Ref);

                    idx++;
                }
            }

            ilg.Emit(OpCodes.Ret);
        }

        private void ImplementConstructor(TypeDescription typeDescription)
        {
            var dbusInterfaces = typeDescription.Interfaces;
            // DBusConnection connection, ObjectPath objectPath, object o, IProxyFactory factory
            var constructor = _typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, s_dbusAdaptorConstructorParameterTypes);
            var ilg = constructor.GetILGenerator();

            {
                // base constructor
                ilg.Emit(OpCodes.Ldarg_0); // this
                ilg.Emit(OpCodes.Ldarg_1); // DBusConnection
                ilg.Emit(OpCodes.Ldarg_2); // ObjectPath
                ilg.Emit(OpCodes.Ldarg_3); // object
                ilg.Emit(OpCodes.Ldarg, 4); // IProxyFactory
                ilg.Emit(OpCodes.Ldarg, 5); // SynchronizationContext
                ilg.Emit(OpCodes.Call, s_baseConstructor);
            }
            
            var introspectionXml = GenerateIntrospectionXml(typeDescription);
            ilg.Emit(OpCodes.Ldarg_0);
            ilg.Emit(OpCodes.Ldstr, introspectionXml);
            ilg.Emit(OpCodes.Stfld, s_setTypeIntrospectionField);

            foreach (var dbusInterface in dbusInterfaces)
            {
                IEnumerable<MethodDescription> methods = dbusInterface.Methods ?? Array.Empty<MethodDescription>();

                var propertyMethods = new[] { dbusInterface.GetPropertyMethod,
                                               dbusInterface.GetAllPropertiesMethod,
                                               dbusInterface.SetPropertyMethod };

                methods = methods.Concat(propertyMethods);

                foreach (var method in methods)
                {
                    if (method == null)
                    {
                        continue;
                    }

                    if (method.IsGenericOut)
                    {
                        throw new NotImplementedException($"Cannot adaptor class for generic method {method.MethodInfo.ToString()}. Refactor the method to return Task<object>.");
                    }

                    var signature = method.InSignature;
                    var memberName = method.Name;
                    bool isPropertyMethod = propertyMethods.Contains(method);
                    string key = isPropertyMethod ? DBusAdapter.GetPropertyAddKey(dbusInterface.Name, memberName, signature) :
                                                    DBusAdapter.GetMethodLookupKey(dbusInterface.Name, memberName, signature);
                    // _methodHandlers.Add(key, GenMethodHandler)
                    {
                        // _methodHandlers
                        ilg.Emit(OpCodes.Ldarg_0);
                        ilg.Emit(OpCodes.Ldfld, s_methodDictionaryField);

                        // key
                        ilg.Emit(OpCodes.Ldstr, key);

                        // value
                        ilg.Emit(OpCodes.Ldarg_0);
                        ilg.Emit(OpCodes.Ldftn, GenMethodHandler(key, method, isPropertyMethod));
                        ilg.Emit(OpCodes.Newobj, s_methodHandlerConstructor);

                        // Add
                        ilg.Emit(OpCodes.Call, s_methodDictionaryAdd);
                    }
                }
            }

            ilg.Emit(OpCodes.Ret);
        }

        private MethodInfo GenSendSignal(SignalDescription signalDescription, bool isPropertiesChangedSignal)
        {
            var key = $"{signalDescription.Interface.Name}.{signalDescription.Name}";
            var method = _typeBuilder.DefineMethod($"Emit{key}".Replace('.', '_'), MethodAttributes.Private, null,
                signalDescription.SignalType == null ? Type.EmptyTypes : new[] { signalDescription.SignalType });

            var ilg = method.GetILGenerator();

            ilg.Emit(OpCodes.Ldarg_0);
            if (isPropertiesChangedSignal)
            {
                ilg.Emit(OpCodes.Ldstr, "org.freedesktop.DBus.Properties");
                ilg.Emit(OpCodes.Ldstr, "PropertiesChanged");
            }
            else
            {
                ilg.Emit(OpCodes.Ldstr, signalDescription.Interface.Name);
                ilg.Emit(OpCodes.Ldstr, signalDescription.Name);
            }

            if (signalDescription.SignalType == null)
            {
                ilg.Emit(OpCodes.Call, s_emitVoidSignal);
            }
            else
            {
                // Signature
                if (isPropertiesChangedSignal)
                {
                    ilg.Emit(OpCodes.Ldstr, "sa{sv}as");
                    ilg.Emit(OpCodes.Newobj, s_signatureConstructor);
                    ilg.Emit(OpCodes.Newobj, s_nullableSignatureConstructor);
                }
                else if (signalDescription.SignalSignature.HasValue)
                {
                    ilg.Emit(OpCodes.Ldstr, signalDescription.SignalSignature.Value.Value);
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

                // Writer
                ilg.Emit(OpCodes.Newobj, s_messageWriterConstructor);

                if (isPropertiesChangedSignal)
                {
                    ilg.Emit(OpCodes.Dup);
                    ilg.Emit(OpCodes.Ldstr, signalDescription.Interface.Name);
                    ilg.Emit(OpCodes.Call, s_writerWriteString);
                    ilg.Emit(OpCodes.Dup);
                    ilg.Emit(OpCodes.Call, s_writerSetSkipNextStructPadding);
                }
                ilg.Emit(OpCodes.Dup);
                ilg.Emit(OpCodes.Ldarg_1);
                ilg.Emit(OpCodes.Call, WriteMethodFactory.CreateWriteMethodForType(signalDescription.SignalType, isCompileTimeType: true));

                ilg.Emit(OpCodes.Call, s_emitNonVoidSignal);
            }

            ilg.Emit(OpCodes.Ret);

            return method;
        }

        private MethodInfo GenMethodHandler(string key, MethodDescription dbusMethod, bool propertyMethod)
        {
            // Task<Message> MethodCall(object o(Ldarg_1), Message methodCall(Ldarg_2), IProxyFactory(Ldarg_3));

            string methodName = $"Handle{key}".Replace('.', '_');
            var method = _typeBuilder.DefineMethod(methodName, MethodAttributes.Private, s_taskOfMessageType, s_methodHandlerParameterTypes);

            var ilg = method.GetILGenerator();

            // call CreateReply

            // this
            ilg.Emit(OpCodes.Ldarg_0);
            // Message
            ilg.Emit(OpCodes.Ldarg_2);
            // Task = (IDbusInterface)object.CallMethod(arguments)
            {
                // (IIinterface)object
                ilg.Emit(OpCodes.Ldarg_1);
                ilg.Emit(OpCodes.Castclass, dbusMethod.Interface.Type);

                // Arguments
                if (dbusMethod.InArguments.Count != 0)
                {
                    // create reader for reading the arguments
                    ilg.Emit(OpCodes.Ldarg_2); // message
                    ilg.Emit(OpCodes.Ldarg_3); // IProxyFactory
                    LocalBuilder reader = ilg.DeclareLocal(s_messageReaderType);
                    ilg.Emit(OpCodes.Newobj, s_messageReaderConstructor); // new MessageReader(message, proxyFactory)
                    ilg.Emit(OpCodes.Stloc, reader);

                    if (propertyMethod)
                    {
                        ilg.Emit(OpCodes.Ldloc, reader);
                        ilg.Emit(OpCodes.Call, s_readerSkipString);
                    }

                    foreach (var argument in dbusMethod.InArguments)
                    {
                        Type parameterType = argument.Type;
                        ilg.Emit(OpCodes.Ldloc, reader);
                        ilg.Emit(OpCodes.Call, ReadMethodFactory.CreateReadMethodForType(parameterType));
                    }
                }

                // Call method
                ilg.Emit(OpCodes.Callvirt, dbusMethod.MethodInfo);
            }

            if (dbusMethod.OutType != null)
            {
                //  Action<MessageWriter, T>
                ilg.Emit(OpCodes.Ldnull);
                ilg.Emit(OpCodes.Ldftn, WriteMethodFactory.CreateWriteMethodForType(dbusMethod.OutType, isCompileTimeType: true));
                var actionConstructor = s_action2GenericType.MakeGenericType(new[] { s_messageWriterType, dbusMethod.OutType }).GetConstructors()[0];
                ilg.Emit(OpCodes.Newobj, actionConstructor);

                // signature
                if (dbusMethod.OutSignature.HasValue)
                {
                    ilg.Emit(OpCodes.Ldstr, dbusMethod.OutSignature.Value.Value);
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

                // CreateReply
                ilg.Emit(OpCodes.Call, s_createNonVoidReply.MakeGenericMethod(new[] { dbusMethod.OutType }));
            }
            else
            {
                // CreateReply
                ilg.Emit(OpCodes.Call, s_createVoidReply);
            }

            ilg.Emit(OpCodes.Ret);

            return method;
        }

        private string GenerateIntrospectionXml(TypeDescription description)
        {
            var writer = new IntrospectionWriter();
            bool hasProperties = false;

            foreach (var interf in description.Interfaces)
            {
                writer.WriteInterfaceStart(interf.Name);
                foreach (var method in interf.Methods)
                {
                    writer.WriteMethodStart(method.Name);
                    foreach (var arg in method.InArguments)
                    {
                        writer.WriteInArg(arg.Name, arg.Signature);
                    }
                    foreach (var arg in method.OutArguments)
                    {
                        writer.WriteOutArg(arg.Name, arg.Signature);
                    }
                    writer.WriteMethodEnd();
                }

                foreach (var signal in interf.Signals)
                {
                    writer.WriteSignalStart(signal.Name);
                    foreach (var arg in signal.SignalArguments)
                    {
                        writer.WriteArg(arg.Name, arg.Signature);
                    }
                    writer.WriteSignalEnd();
                }

                foreach (var prop in interf.Properties)
                {
                    hasProperties = true;
                    writer.WriteProperty(prop.Name, prop.Signature, prop.Access);
                }
                writer.WriteInterfaceEnd();
            }
            if (hasProperties)
            {
                writer.WritePropertiesInterface();
            }
            writer.WriteIntrospectableInterface();
            writer.WritePeerInterface();

            return writer.ToString();
        }
    }
}
