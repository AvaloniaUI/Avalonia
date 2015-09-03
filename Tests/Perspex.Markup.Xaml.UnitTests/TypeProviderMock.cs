namespace Perspex.Xaml.Base.UnitTest
{
    using OmniXaml;
    using System;

    class TypeProviderMock : ITypeProvider
    {
        private readonly string typeName;
        private readonly string clrNamespace;
        private readonly string assemblyName;
        private readonly Type typeToReturn;

        public TypeProviderMock(string typeName, string clrNamespace, string assemblyName, Type typeToReturn)
        {
            this.typeName = typeName;
            this.clrNamespace = clrNamespace;
            this.assemblyName = assemblyName;
            this.typeToReturn = typeToReturn;
        }

        public TypeProviderMock()
        {
        }

        public Type GetType(string typeName, string clrNamespace, string assemblyName)
        {
            if (this.typeName == typeName && this.clrNamespace == clrNamespace && this.assemblyName == assemblyName)
            {
                return typeToReturn;
            }            

            throw new TypeNotFoundException("The Type cannot be found");
        }
    }
}