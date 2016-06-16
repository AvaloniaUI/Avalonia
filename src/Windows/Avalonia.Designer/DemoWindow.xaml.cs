using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace Avalonia.Designer
{

    internal partial class DemoWindow
    {
        public DemoWindow()
        {
            InitializeComponent();
        }

        public DemoWindow(string targetExe, string targetPath, string sourceAssembly) : this()
        {
            
            if (targetPath != null)
            {
                Xaml.Text = File.ReadAllText(targetPath);
            }
            SourceAssembly.Text = sourceAssembly ?? targetExe;
            TargetExe.Text = targetExe;
        }

        static string OpenFile(string filter)
        {
            var dlg = new OpenFileDialog {Filter = filter};
            if (dlg.ShowDialog() == true)
                return dlg.FileName;
            return null;
        }

        private void SelectExeClicked(object sender, RoutedEventArgs e)
        {
            var exe = OpenFile("exe|*.exe");
            if (exe != null)
                TargetExe.Text = exe;
        }

        private void RestartClicked(object sender, RoutedEventArgs e)
        {
            Designer.RestartProcess();
        }

        private void SelectSourceClicked(object sender, RoutedEventArgs e)
        {
            var exe = OpenFile("assembly|*.exe,*.dll");
            if (exe != null)
                SourceAssembly.Text = exe;
        }
    }
}
