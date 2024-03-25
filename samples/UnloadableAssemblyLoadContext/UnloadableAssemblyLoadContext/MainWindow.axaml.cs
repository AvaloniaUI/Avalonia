using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Markup.Xaml.XamlIl.Runtime;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;

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
        if (Debugger.IsAttached)
        {
            this.AttachDevTools();
        }
    }
    private PlugTool _plugTool;
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        test();
        Content = _plugTool.FindControl("UnloadableAssemblyLoadContextPlug.TestControl");


    }
    public  T? GetChildOfType<T>(Control control)
        where T : Control
    {
        var queue = new Queue<Control>();
        queue.Enqueue(control);

        while (queue.Count > 0)
        {
            var currentControl = queue.Dequeue();
            foreach (var child in currentControl.GetVisualChildren())
            {
                var childControl = child as Control;
                if (childControl != null)
                {
                    var childControlStyles = childControl.Styles;
                    if (childControlStyles.Count>1)
                    {
                        
                    }
                    queue.Enqueue(childControl);
                }
            }
        }

        return null;
    }
    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        GetChildOfType<Control>(this);
        
        
        Thread.CurrentThread.IsBackground = false;
        var weakReference = _plugTool.Unload();
        while (weakReference.IsAlive)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Thread.Sleep(100);
        }

        Console.WriteLine("Done");
       
        
    }

    public static IStyle Style;
    public  void test(){
        
        //Notice : 你可以删除UnloadableAssemblyLoadContextPlug.dll所在文件夹中有关Avalonia的所有Dll,但这不是必须的
        //Notice : You can delete all Dlls about Avalonia in the folder where UnloadableAssemblyLoadContextPlug.dll is located, but this is not necessary
        FileInfo fileInfo = new FileInfo("..\\..\\..\\..\\UnloadableAssemblyLoadContextPlug\\bin\\Debug\\net7.0\\UnloadableAssemblyLoadContextPlug.dll");
        var AssemblyLoadContextH = new AssemblyLoadContextH(fileInfo.FullName,"test");
        
        var assembly = AssemblyLoadContextH.LoadFromAssemblyPath(fileInfo.FullName);
        
        _plugTool=new PlugTool();
        _plugTool.AssemblyLoadContextH = AssemblyLoadContextH;
      
        var styles = new Styles();
        var styleInclude = new StyleInclude(new Uri("avares://UnloadableAssemblyLoadContextPlug", UriKind.Absolute));
        styleInclude.Source=new Uri("ControlStyle.axaml", UriKind.Relative);
        styles.Add(styleInclude);
        Style = styles;
        Application.Current.Styles.Add(styles);
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
    public class MyServiceProvider : IServiceProvider
    {
        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(INameScope))
            {
                if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    var nameScope = NameScope.GetNameScope((StyledElement)desktop.MainWindow);
                    return nameScope;
                }

                return null;
            }
                
            return null;
        }
    }
    
    /*
      XamlIlContext.Context<App> context = new XamlIlContext.Context<App>(obj0, new object[1]
      {
        (object) !AvaloniaResources.NamespaceInfo:/App.axaml.Singleton
      }, "avares://UnloadableAssemblyLoadContext/App.axaml");
      context.RootObject = obj1;
      context.IntermediateRoot = (object) obj1;
      App app1;
      App app2 = app1 = obj1;
      context.PushParent((object) app2);
      App app3 = app2;
      app3.RequestedThemeVariant = ThemeVariant.Default;
      app3.Styles.Add((IStyle) !AvaloniaResources.Build:/Styles1.axaml(XamlIlRuntimeHelpers.CreateRootServiceProviderV3((IServiceProvider) context)));
     */
}
