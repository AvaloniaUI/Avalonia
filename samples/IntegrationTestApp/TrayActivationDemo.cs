using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using MiniMvvm;

namespace IntegrationTestApp;

// Opt-in additions to the existing tray, leaving the normal checkbox tests intact.
internal static class TrayActivationDemo
{
    public static void Attach(App app)
    {
        Window? target = null;
        Window GetTarget()
        {
            if (target != null)
                return target;
            target = new Window
            {
                Title = "Tray activation target", Width = 360, Height = 180,
                Content = new TextBlock { Text = "Hide, close and reopen me using the tray menu.", Margin = new(20) }
            };
            target.Activated += (_, _) => Console.WriteLine("Tray target: activated");
            target.Deactivated += (_, _) => Console.WriteLine("Tray target: deactivated");
            target.Closed += (_, _) => { target = null; Console.WriteLine("Tray target: closed"); };
            return target;
        }
        void Show()
        {
            var window = GetTarget();
            window.ShowActivated = true;
            window.Show();
            window.Activate();
        }
        var tray = TrayIcon.GetIcons(app)![0];
        tray.Clicked += (_, _) => Show();
        var menu = new NativeMenu();
        void Add(string label, Action action, bool click = true, bool command = false)
        {
            var item = new NativeMenuItem(label);
            if (click) item.Click += (_, _) => action();
            if (command) item.Command = MiniCommand.Create(action);
            menu.Items.Add(item);
        }
        Add("Show (Click)", Show);
        Add("Show (Command)", Show, click: false, command: true);
        Add("Show (Click and Command)", Show, command: true);
        Add("Show (post)", () => Dispatcher.UIThread.Post(Show));
        Add("Show (await)", async () => { await Task.Yield(); Show(); });
        Add("Show without activation", () => { var window = GetTarget(); window.ShowActivated = false; window.Show(); });
        Add("Activate", () => target?.Activate());
        Add("Hide", () => target?.Hide());
        Add("Close", () => target?.Close());
        tray.Menu!.Items.Add(new NativeMenuItem("Activation") { Menu = menu });
    }
}
