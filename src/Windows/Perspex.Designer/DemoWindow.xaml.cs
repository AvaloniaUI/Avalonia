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

namespace Perspex.Designer
{

    internal partial class DemoWindow
    {
        public DemoWindow()
        {
            InitializeComponent();
        }

        public DemoWindow(string targetExe, string targetPath) : this()
        {
            TargetExe.Text = targetExe;
            if (targetExe != null)
                Xaml.Text = File.ReadAllText(targetPath);
        }
    }
}
