using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Diagnostics;
using Avalonia.Gtk.Embedding;
using ControlCatalog;
using Gtk;

namespace GtkInteropDemo
{
    class MainWindow : Window
    {
        public MainWindow() : base("Gtk Embedding Demo")
        {
            var root = new HBox();
            var left  = new VBox();
            left.Add(new Button("I'm GTK button"));
            left.Add(new Calendar());
            root.PackEnd(left, false, false, 0);
            root.PackStart(new GtkAvaloniaControlHost() {Content = new ControlCatalogControl()}, true, true, 0);
            Add(root);
            
            ShowAll();
        }
    }
}
