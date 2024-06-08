// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Linq;
using System.Reflection;

namespace Tmds.DBus.CodeGen
{
    static class TypeExtensions
    {
        public static ConstructorInfo GetConstructor(this Type type, BindingFlags bindingFlags, Type[] types)
        {
            var constructors = type.GetConstructors(bindingFlags);
            return (from constructor in constructors
                    let parameters = constructor.GetParameters()
                    let parameterTypes = parameters.Select(p => p.ParameterType).ToArray()
                    where types.SequenceEqual(parameterTypes)
                    select constructor).Single();           
        }
    }
}