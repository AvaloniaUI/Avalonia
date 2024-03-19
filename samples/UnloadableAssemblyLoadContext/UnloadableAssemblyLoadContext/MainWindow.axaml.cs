using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace UnloadableAssemblyLoadContext;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
    private UnloadTool unloadTool;
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        test();
        
        
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
       
        Thread.CurrentThread.IsBackground = false;
        var weakReference = unloadTool.Unload();
        while (weakReference.IsAlive)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Thread.Sleep(100);
        }

        Console.WriteLine("Done");
       
        
    }
    

    public  void test(){
        
        //Notice : 你可以删除UnloadableAssemblyLoadContextPlug.dll所在文件夹中有关Avalonia的所有Dll,但这不是必须的
        //Notice : You can delete all Dlls about Avalonia in the folder where UnloadableAssemblyLoadContextPlug.dll is located, but this is not necessary
        FileInfo fileInfo = new FileInfo("..\\..\\..\\..\\UnloadableAssemblyLoadContextPlug\\bin\\Debug\\net7.0\\UnloadableAssemblyLoadContextPlug.dll");
        var AssemblyLoadContextH = new AssemblyLoadContextH(fileInfo.FullName,"test");
        var assembly = AssemblyLoadContextH.LoadFromAssemblyPath(fileInfo.FullName);
        unloadTool=new UnloadTool();
        unloadTool.AssemblyLoadContextH = AssemblyLoadContextH;
        foreach (var type in assembly.GetTypes())
        {
            if (type.FullName=="AvaloniaPlug.Window1")
            {
                
                //创建type实例
                Window instance = (Window)type.GetConstructor( new Type[0]).Invoke(null);
                
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    instance.Show();
                    instance.Close();
                            
                }).Wait();
                
                instance = null;
                
                //instance.Show();

            }
        }
        
    }
}
