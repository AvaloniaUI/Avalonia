using System;
using System.Collections.Generic;
using System.Threading.Tasks.Dataflow;
using Avalonia.Rendering.Composition.Transport;
using Xunit;

namespace Avalonia.Base.UnitTests.Composition;

public class BatchStreamTests
{
    [Fact]
    public void BatchStreamCorrectlyWritesAndReadsData()
    {
        var data = new BatchStreamData();
        var memPool = new BatchStreamMemoryPool(false, 100, _ => { });
        var objPool = new BatchStreamObjectPool<object>(false, 10, _ => { });

        var guids = new List<Guid>();
        var objects = new List<object>();
        for (var c = 0; c < 453; c++)
        {
            guids.Add(Guid.NewGuid());
            objects.Add(new object());
        }

        using (var writer = new BatchStreamWriter(data, memPool, objPool))
        {
            foreach(var guid in guids)
                writer.Write(guid);
            foreach (var obj in objects)
                writer.WriteObject(obj);
        }

        using (var reader = new BatchStreamReader(data, memPool, objPool))
        {
            foreach (var guid in guids) 
                Assert.Equal(guid, reader.Read<Guid>());
            foreach (var obj in objects)
                Assert.Equal(obj, reader.ReadObject());
        }
        


    }
}