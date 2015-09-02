// -----------------------------------------------------------------------
// <copyright file="LogInViewModel.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace GitHubClient.ViewModels
{
    using System;
    using Octokit;
    using ReactiveUI;

    public class LogInViewModel : ReactiveObject
    {
        private string username;

        public LogInViewModel()
        {
            this.OkCommand = ReactiveCommand.Create(
                this.WhenAnyValue(
                    x => x.Username,
                    x => !string.IsNullOrWhiteSpace(x)));
        }

        public string Username
        {
            get { return this.username; }
            set { this.RaiseAndSetIfChanged(ref this.username, value); }
        }

        public ReactiveCommand<object> OkCommand
        {
            get;
            private set;
        }
    }
}