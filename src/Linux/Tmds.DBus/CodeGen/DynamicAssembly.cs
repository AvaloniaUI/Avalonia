// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo(Tmds.DBus.Connection2.DynamicAssemblyName)]

namespace Tmds.DBus.CodeGen
{
    internal class DynamicAssembly
    {
        public static readonly DynamicAssembly Instance = new DynamicAssembly();

        private readonly AssemblyBuilder _assemblyBuilder;
        private readonly ModuleBuilder _moduleBuilder;
        private readonly Dictionary<Type, TypeInfo> _proxyTypeMap;
        private readonly Dictionary<Type, TypeInfo> _adapterTypeMap;
        private readonly object _gate = new object();

        private DynamicAssembly()
        {
            byte[] keyBuffer;
            using (Stream keyStream = typeof(DynamicAssembly).GetTypeInfo().Assembly.GetManifestResourceStream("Tmds.DBus.sign.snk"))
            {
                if (keyStream == null)
                {
                    throw new InvalidOperationException("'Tmds.DBus.sign.snk' not found in resources");
                }
                keyBuffer = new byte[keyStream.Length];
                keyStream.Read(keyBuffer, 0, keyBuffer.Length);
            }

            var dynamicAssemblyName = "Tmds.DBus.Emit";
            var assemblyName = new AssemblyName(dynamicAssemblyName);
            assemblyName.Version = new Version(1, 0, 0);
            assemblyName.Flags = AssemblyNameFlags.PublicKey;
            assemblyName.SetPublicKey(keyBuffer);
            _assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            _moduleBuilder = _assemblyBuilder.DefineDynamicModule(dynamicAssemblyName);
            _proxyTypeMap = new Dictionary<Type, TypeInfo>();
            _adapterTypeMap = new Dictionary<Type, TypeInfo>();
        }

        public TypeInfo GetProxyTypeInfo(Type interfaceType)
        {
            TypeInfo typeInfo;            
            lock (_proxyTypeMap)
            {
                if (_proxyTypeMap.TryGetValue(interfaceType, out typeInfo))
                {
                    return typeInfo;
                }
            }

            lock (_gate)
            {
                lock (_proxyTypeMap)
                {
                    if (_proxyTypeMap.TryGetValue(interfaceType, out typeInfo))
                    {
                        return typeInfo;
                    }
                }

                typeInfo = new DBusObjectProxyTypeBuilder(_moduleBuilder).Build(interfaceType);

                lock (_proxyTypeMap)
                {
                    _proxyTypeMap[interfaceType] = typeInfo;
                }

                return typeInfo;
            }
        }

        public TypeInfo GetExportTypeInfo(Type objectType)
        {
            TypeInfo typeInfo;

            lock (_adapterTypeMap)
            {
                if (_adapterTypeMap.TryGetValue(objectType, out typeInfo))
                {
                    return typeInfo;
                }
            }

            lock (_gate)
            {
                lock (_adapterTypeMap)
                {
                    if (_adapterTypeMap.TryGetValue(objectType, out typeInfo))
                    {
                        return typeInfo;
                    }
                }

                typeInfo = new DBusAdapterTypeBuilder(_moduleBuilder).Build(objectType);

                lock (_adapterTypeMap)
                {
                    _adapterTypeMap[objectType] = typeInfo;
                }

                return typeInfo;
            }
        }
    }
}
