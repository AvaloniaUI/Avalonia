using System;
using Avalonia.Collections.Pooled;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Utilities;

// Special license applies <see href="https://raw.githubusercontent.com/AvaloniaUI/Avalonia/master/src/Avalonia.Base/Rendering/Composition/License.md">License.md</see>

namespace Avalonia.Rendering.Composition.Drawing;

/// <summary>
/// A list of serialized drawing commands
/// </summary>
internal class CompositionDrawList : PooledList<IRef<IDrawOperation>>
{
    public Size? Size { get; set; }
    
    public CompositionDrawList()
    {
        
    }

    public CompositionDrawList(int capacity) : base(capacity)
    {
        
    }
    
    public override void Dispose()
    {
        foreach(var item in this)
            item.Dispose();
        base.Dispose();
    }

    public CompositionDrawList Clone()
    {
        var clone = new CompositionDrawList(Count) { Size = Size };
        foreach (var r in this)
            clone.Add(r.Clone());
        return clone;
    }

    public void Render(CompositorDrawingContextProxy canvas)
    {
        foreach (var cmd in this)
        {
            canvas.VisualBrushDrawList = (cmd.Item as BrushDrawOperation)?.Aux as CompositionDrawList;
            cmd.Item.Render(canvas);
        }

        canvas.VisualBrushDrawList = null;
    }
}

/// <summary>
/// An helper class for building <see cref="CompositionDrawList"/>
/// </summary>
internal class CompositionDrawListBuilder
{
    private CompositionDrawList? _operations;
    private bool _owns;

    public void Reset(CompositionDrawList? previousOperations)
    {
        _operations = previousOperations;
        _owns = false;
    }

    public int Count => _operations?.Count ?? 0;
    public CompositionDrawList? DrawOperations => _operations;

    void MakeWritable(int atIndex)
    {
        if(_owns)
            return;
        _owns = true;
        var newOps = new CompositionDrawList(_operations?.Count ?? Math.Max(1, atIndex));
        if (_operations != null)
        {
            for (var c = 0; c < atIndex; c++)
                newOps.Add(_operations[c].Clone());
        }

        _operations = newOps;
    }

    public void ReplaceDrawOperation(int index, IDrawOperation node)
    {
        MakeWritable(index);
        DrawOperations!.Add(RefCountable.Create(node));
    }

    public void AddDrawOperation(IDrawOperation node)
    {
        MakeWritable(Count);
        DrawOperations!.Add(RefCountable.Create(node));
    }

    public void TrimTo(int count)
    {
        if (count < Count)
            _operations!.RemoveRange(count, _operations.Count - count);
    }
}
