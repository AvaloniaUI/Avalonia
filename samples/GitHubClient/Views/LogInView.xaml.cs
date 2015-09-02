// -----------------------------------------------------------------------
// <copyright file="LogInView.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace GitHubClient.Views
{
    using System;
    using ViewModels;
    using Perspex.Controls;
    using Perspex.Markup.Xaml;

    public class LogInView : UserControl
    {
        public LogInView()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            PerspexXamlLoader.Load(this);
        }
    }
}
