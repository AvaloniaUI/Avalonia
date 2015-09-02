// -----------------------------------------------------------------------
// <copyright file="MainWindow.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace GitHubClient.Views
{
    using System;
    using ViewModels;
    using Perspex.Controls;
    using Perspex.Markup.Xaml;

    public class MainWindow : Window
    {
        MainWindowViewModel viewModel = new MainWindowViewModel();

        public MainWindow()
        {
            this.InitializeComponent();
            this.DataContext = viewModel;
            App.AttachDevTools(this);
        }

        private void InitializeComponent()
        {
            PerspexXamlLoader.Load(this);
        }
    }
}
