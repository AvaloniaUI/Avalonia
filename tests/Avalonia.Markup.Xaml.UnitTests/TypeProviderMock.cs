// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using OmniXaml;
using System;

namespace Avalonia.Markup.Xaml.UnitTests
{
    internal class TypeProviderMock : ITypeProvider
    {
        private readonly string _typeName;
        private readonly string _clrNamespace;
        private readonly string _assemblyName;
        private readonly Type _typeToReturn;

        public TypeProviderMock(string typeName, string clrNamespace, string assemblyName, Type typeToReturn)
        {
            _typeName = typeName;
            _clrNamespace = clrNamespace;
            _assemblyName = assemblyName;
            _typeToReturn = typeToReturn;
        }

        public TypeProviderMock()
        {
        }

        public Type GetType(string typeName, string clrNamespace, string assemblyName)
        {
            if (_typeName == typeName && _clrNamespace == clrNamespace && _assemblyName == assemblyName)
            {
                return _typeToReturn;
            }

            throw new TypeNotFoundException("The Type cannot be found");
        }
    }
}