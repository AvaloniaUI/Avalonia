﻿// -----------------------------------------------------------------------
// <copyright file="LogInViewModel.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Xaml.Base.UnitTest.SampleModel
{
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