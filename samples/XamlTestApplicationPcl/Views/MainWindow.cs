// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex;
using Perspex.Controls;
using Perspex.Diagnostics;
using Perspex.Markup.Xaml;
using XamlTestApplication.ViewModels;

namespace XamlTestApplication.Views
{
    public class MainWindow : Window
    {
        private MenuItem _exitMenu;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
            DevTools.Attach(this);
        }

        private void InitializeComponent()
        {
            PerspexXamlLoader.Load(this);
            _exitMenu = this.FindControl<MenuItem>("exitMenu");
            _exitMenu.Click += (s, e) => Application.Current.Exit();
        }
    }
}