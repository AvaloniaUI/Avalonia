// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;

namespace Tmds.DBus.CodeGen
{
    internal class InterfaceDescription
    {
        public InterfaceDescription(Type type, string name, IList<MethodDescription> methods, IList<SignalDescription> signals,
            IList<PropertyDescription> properties, MethodDescription propertyGetMethod, MethodDescription propertyGetAllMethod, MethodDescription propertySetMethod,
            SignalDescription propertiesChangedSignal)
        {
            Type = type;
            Name = name;
            _methods = methods;
            _signals = signals;
            GetAllPropertiesMethod = propertyGetAllMethod;
            SetPropertyMethod = propertySetMethod;
            GetPropertyMethod = propertyGetMethod;
            PropertiesChangedSignal = propertiesChangedSignal;
            _properties = properties;

            foreach (var signal in Signals)
            {
                signal.Interface = this;
            }
            if (propertiesChangedSignal != null)
            {
                PropertiesChangedSignal.Interface = this;
            }
            foreach (var method in Methods)
            {
                method.Interface = this;
            }
            foreach (var method in new[] { GetPropertyMethod,
                                           SetPropertyMethod,
                                           GetAllPropertiesMethod})
            {
                if (method != null)
                {
                    method.Interface = this;
                }
            }
        }

        public Type Type { get; }
        public string Name { get; }
        private IList<MethodDescription> _methods;
        public IList<MethodDescription> Methods { get { return _methods ?? Array.Empty<MethodDescription>(); } }
        private IList<SignalDescription> _signals;
        public IList<SignalDescription> Signals { get { return _signals ?? Array.Empty<SignalDescription>(); } }
        public MethodDescription GetPropertyMethod { get; }
        public MethodDescription GetAllPropertiesMethod { get; }
        public MethodDescription SetPropertyMethod { get; }
        public SignalDescription PropertiesChangedSignal { get; }
        private IList<PropertyDescription> _properties;
        public IList<PropertyDescription> Properties { get { return _properties ?? Array.Empty<PropertyDescription>(); } }
    }
}
