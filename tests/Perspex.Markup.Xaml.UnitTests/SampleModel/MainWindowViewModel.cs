// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Xaml.Base.UnitTest.SampleModel;
using ReactiveUI;

namespace GitHubClient.ViewModels
{
    public class MainWindowViewModel : ReactiveObject
    {
        private object _content;

        private LogInViewModel _login;

        public MainWindowViewModel()
        {
            ShowLogin();
        }

        public object Content
        {
            get { return _content; }
            set { this.RaiseAndSetIfChanged(ref _content, value); }
        }

        private void ShowLogin()
        {
            _login = new LogInViewModel();
            _login.OkCommand.Subscribe(_ => ShowRepositories());
            Content = _login;
        }

        private void ShowRepositories()
        {
            var vm = new UserRepositoriesViewModel();
            var task = vm.Load(_login.Username);
            Content = vm;
        }
    }
}