using Avalonia.Utilities;
using Xunit;

namespace Avalonia.Base.UnitTests.Utilities;

public class WeakHashListTests
{
    [Fact]
    public void Is_Empty_Works()
    {
        var target = new WeakHashList<string>();
        
        Assert.True(target.IsEmpty);
        
        target.Add("1");
        
        Assert.False(target.IsEmpty);
        
        target.Remove("1");
        
        Assert.True(target.IsEmpty);

        // Fill array storage.
        var arrMaxSize = WeakHashList<string>.DefaultArraySize;

        for (int i = 0; i < arrMaxSize; i++)
        {
            target.Add(i.ToString());
        }
        
        Assert.False(target.IsEmpty);
        
        // This goes above array storage and upgrades to a dictionary.
        target.Add(arrMaxSize.ToString());
        
        Assert.False(target.IsEmpty);

        // Remove everything, this should still keep an empty dictionary.
        for (int i = 0; i < arrMaxSize + 1; i++)
        {
            target.Remove(i.ToString());
        }
        
        Assert.True(target.IsEmpty);
    }

    [Fact]
    public void Array_Compact_After_Remove_Works()
    {
        var target = new WeakHashList<string>();
        
        // Use all slots in array storage.
        var arrMaxSize = WeakHashList<string>.DefaultArraySize;

        for (int i = 0; i < arrMaxSize; i++)
        {
            target.Add(i.ToString());
        }
        
        // This should compact the array.
        target.Remove("3");
        
        // And new value should fill empty space.
        target.Add("42");
    }
}
