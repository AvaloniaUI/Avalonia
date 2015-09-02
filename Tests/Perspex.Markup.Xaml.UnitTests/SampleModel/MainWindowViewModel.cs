// -----------------------------------------------------------------------
// <copyright file="MainWindowViewModel.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace GitHubClient.ViewModels
{
    using System;
    using Perspex.Xaml.Base.UnitTest.SampleModel;
    using ReactiveUI;

    public class MainWindowViewModel : ReactiveObject
    {
        private object content;

        private LogInViewModel login;

        public MainWindowViewModel()
        {
            this.ShowLogin();
        }

        public object Content
        {
            get { return this.content; }
            set { this.RaiseAndSetIfChanged(ref this.content, value); }
        }

        private void ShowLogin()
        {
            this.login = new LogInViewModel();
            this.login.OkCommand.Subscribe(_ => this.ShowRepositories());
            this.Content = this.login;
        }

        private void ShowRepositories()
        {
            var vm = new UserRepositoriesViewModel();
            var task = vm.Load(this.login.Username);
            this.Content = vm;
        }
    }
}