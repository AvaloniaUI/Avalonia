using System;
using System.IO;
using System.Reflection;
using Microsoft.Build.Utilities;
using Xunit;

namespace Avalonia.Build.Tasks.UnitTest;

public class GenerateAvaloniaResourcesTaskTest
{
    [Fact]
    public void Does_Support_LinkBase_In_Avalonia_Resources_Generator()
    {
        var path = "path/to/resources";
        var expected = "/" + path;
        
        var basePath = Path.Combine(Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath), "Assets");
        var rootPath = Path.Combine(basePath, "root.xml");
        
        var avaloniaResourceItem = new TaskItem();
        avaloniaResourceItem.SetMetadata("LinkBase", path);
        var source = new GenerateAvaloniaResourcesTask.Source(avaloniaResourceItem, rootPath);
        
        Assert.Equal(expected, source.Path);
    }
}
