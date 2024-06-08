// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using System.Reflection;
using Tmds.DBus.Protocol;

namespace Tmds.DBus.CodeGen
{
    internal class MethodDescription
    {
        public MethodDescription(MethodInfo member, string name, IList<ArgumentDescription> inArguments, Signature? inSignature, Type outType, bool isGenericOut, Signature? outSignature, IList<ArgumentDescription> outArguments)
        {
            MethodInfo = member;
            Name = name;
            _inArguments = inArguments;
            InSignature = inSignature;
            OutType = outType;
            IsGenericOut = isGenericOut;
            OutSignature = outSignature;
            _outArguments = outArguments;
        }

        public InterfaceDescription Interface { get; internal set; }
        public MethodInfo MethodInfo { get; }
        public string Name { get; }
        public Type OutType { get; }
        public Signature? OutSignature { get; }
        private IList<ArgumentDescription> _outArguments;
        public IList<ArgumentDescription> OutArguments { get { return _outArguments ?? Array.Empty<ArgumentDescription>(); } }
        private IList<ArgumentDescription> _inArguments;
        public IList<ArgumentDescription> InArguments { get { return _inArguments ?? Array.Empty<ArgumentDescription>(); } }
        public Signature? InSignature { get; }
        public bool IsGenericOut { get; }

    }
}