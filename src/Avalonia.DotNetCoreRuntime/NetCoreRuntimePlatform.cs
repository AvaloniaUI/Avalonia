using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.DotNet.PlatformAbstractions;
using Microsoft.Extensions.DependencyModel;

namespace Avalonia.Shared.PlatformSupport
{
    internal partial class StandardRuntimePlatform
    {
        private static readonly Lazy<Assembly[]> Assemblies = new Lazy<Assembly[]>(LoadAssemblies);
        public Assembly[] GetLoadedAssemblies() => Assemblies.Value;

        static Assembly[] LoadAssemblies()
        {
            var assemblies = new List<Assembly>();
            // Mostly copy-pasted from (MIT):
            // https://github.com/StefH/System.AppDomain.Core/blob/0b35e676c2721aa367b96e62eb52c97ee0b43a70/src/System.AppDomain.NetCoreApp/AppDomain.cs

            foreach (var assemblyName in
                DependencyContext.Default.GetRuntimeAssemblyNames(RuntimeEnvironment.GetRuntimeIdentifier()))
            {
                try
                {
                    var assembly = Assembly.Load(assemblyName);
                    // just load all types and skip this assembly if one or more types cannot be resolved
                    assembly.DefinedTypes.ToArray();
                    assemblies.Add(assembly);
                }
                catch (Exception ex)
                {
                    Debug.Write(ex.Message);
                }
            }
            return assemblies.ToArray();
        }
    }
}
