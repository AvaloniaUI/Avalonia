// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using Tmds.DBus.Protocol;

namespace Tmds.DBus.CodeGen
{
    internal class PropertyDescription
    {
        public PropertyDescription(string name, Signature type, PropertyAccess access)
        {
            Name = name;
            Signature = type;
            Access = access;
        }

        public string Name { get; }
        public Signature Signature { get; }
        public PropertyAccess Access { get; }

    }
}
