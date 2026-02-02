using System;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Rendering;

namespace ControlCatalog
{
    public partial class MainWindow : Window
    {
        private NativeMenu? _recentMenu;

        public MainWindow()
        {
            InitializeComponent();

            _recentMenu = ((NativeMenu.GetMenu(this)?.Items[0] as NativeMenuItem)?.Menu?.Items[2] as NativeMenuItem)?.Menu;
        }

        public static string MenuQuitHeader => RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "Quit Avalonia" : "E_xit";

        public static KeyGesture MenuQuitGesture => RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ?
            new KeyGesture(Key.Q, KeyModifiers.Meta) :
            new KeyGesture(Key.F4, KeyModifiers.Alt);

        public void OnOpenClicked(object sender, EventArgs args)
        {
            _recentMenu?.Items.Insert(0, new NativeMenuItem("Item " + (_recentMenu.Items.Count + 1)));
        }

        public void OnCloseClicked(object sender, EventArgs args)
        {
            Close();
        }
        
        protected override void OnKeyDown(KeyEventArgs e)
        {
            
            var flag = e.Key switch
            {
                Key.F5 => RendererDebugOverlays.Fps,
                Key.F6 => RendererDebugOverlays.DirtyRects,
                Key.F7 => RendererDebugOverlays.RenderTimeGraph,
                Key.F8 => RendererDebugOverlays.LayoutTimeGraph,
                    
                _ => default(RendererDebugOverlays)
            };
            if(RendererDiagnostics.DebugOverlays.HasFlag(flag))
                RendererDiagnostics.DebugOverlays &= ~flag;
            else
                RendererDiagnostics.DebugOverlays |= flag;

            base.OnKeyDown(e);
        }
    }
}
