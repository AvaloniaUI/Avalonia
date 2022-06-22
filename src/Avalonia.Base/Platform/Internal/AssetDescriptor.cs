using System;
using System.IO;
using System.Reflection;

namespace Avalonia.Platform.Internal;

internal interface IAssetDescriptor
{
    Stream GetStream();
    Assembly Assembly { get; }
}

internal class AssemblyResourceDescriptor : IAssetDescriptor
{
    private readonly Assembly _asm;
    private readonly string _name;

    public AssemblyResourceDescriptor(Assembly asm, string name)
    {
        _asm = asm;
        _name = name;
    }

    public Stream GetStream()
    {
        var s = _asm.GetManifestResourceStream(_name);
        return s ?? throw new InvalidOperationException($"Could not find manifest resource stream '{_name}',");
    }

    public Assembly Assembly => _asm;
}
        
internal class AvaloniaResourceDescriptor : IAssetDescriptor
{
    private readonly int _offset;
    private readonly int _length;
    public Assembly Assembly { get; }

    public AvaloniaResourceDescriptor(Assembly asm, int offset, int length)
    {
        _offset = offset;
        _length = length;
        Assembly = asm;
    }

    public Stream GetStream()
    {
        var s = Assembly.GetManifestResourceStream(Constants.AvaloniaResourceName) ??
                throw new InvalidOperationException($"Could not find manifest resource stream '{Constants.AvaloniaResourceName}',");
        return new SlicedStream(s, _offset, _length);
    }
}
