using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.XamlIl;

namespace Avalonia.Designer.HostApp;

class DesignXamlLoader : AvaloniaXamlLoader.IRuntimeXamlLoader
{
    public object Load(RuntimeXamlLoaderDocument document, RuntimeXamlLoaderConfiguration configuration)
    {
        PreloadDepsAssemblies(configuration.LocalAssembly ?? Assembly.GetEntryAssembly());
        
        return AvaloniaXamlIlRuntimeCompiler.Load(document, configuration);
    }

    private void PreloadDepsAssemblies(Assembly targetAssembly)
    {
        // Assemblies loaded in memory (e.g. single file) return empty string from Location.
        // In these cases, don't try probing next to the assembly.
        var assemblyLocation = targetAssembly.Location;
        if (string.IsNullOrEmpty(assemblyLocation))
        {
            return;
        }

        var depsJsonFile = Path.ChangeExtension(assemblyLocation, ".deps.json");
        if (!File.Exists(depsJsonFile))
        {
            return;
        }

        using var stream = File.OpenRead(depsJsonFile);

        /*
         We can't use any references in the Avalonia.Designer.HostApp. Including even json.
         Ideally we would prefer Microsoft.Extensions.DependencyModel package, but can't use it here.
         So, instead we need to fallback to some JSON parsing using pretty easy regex.
         
         Json part example:
"Avalonia.Xaml.Interactions/11.0.0-preview5": {
  "dependencies": {
    "Avalonia": "11.0.999",
    "Avalonia.Xaml.Interactivity": "11.0.0-preview5"
  },
  "runtime": {
    "lib/net6.0/Avalonia.Xaml.Interactions.dll": {
      "assemblyVersion": "11.0.0.0",
      "fileVersion": "11.0.0.0"
    }
  }
},
        We want to extract "lib/net6.0/Avalonia.Xaml.Interactions.dll" from here.
        No need to resolve real path of ref assemblies.
        No need to handle special cases with .NET Framework and GAC.
         */
        var text = new StreamReader(stream).ReadToEnd();
        var matches = Regex.Matches( text, """runtime"\s*:\s*{\s*"([^"]+)""");

        foreach (Match match in matches)
        {
            if (match.Groups[1] is { Success: true } g)
            {
                var assemblyName = Path.GetFileNameWithoutExtension(g.Value);
                try
                {
                    _ = Assembly.Load(new AssemblyName(assemblyName));
                }
                catch
                {
                }
            }
        }
    }
}
