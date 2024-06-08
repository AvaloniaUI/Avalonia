// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using System.Reflection;
using Tmds.DBus.Protocol;

namespace Tmds.DBus.CodeGen
{
    internal class SignalDescription
    {
        public SignalDescription(MethodInfo method, string name, Type actionType, Type signalType, Signature? signature, IList<ArgumentDescription> arguments, bool hasOnError)
        {
            MethodInfo = method;
            Name = name;
            ActionType = actionType;
            SignalType = signalType;
            SignalSignature = signature;
            _signalArguments = arguments;
            HasOnError = hasOnError;
        }

        public InterfaceDescription Interface { get; internal set; }
        public MethodInfo MethodInfo { get; }
        public string Name { get; }
        public Type SignalType { get; }
        public Signature? SignalSignature { get; }
        private IList<ArgumentDescription> _signalArguments;
        public IList<ArgumentDescription> SignalArguments { get { return _signalArguments ?? Array.Empty<ArgumentDescription>(); } }
        public Type ActionType { get; }
        public bool HasOnError { get; }
    }
}