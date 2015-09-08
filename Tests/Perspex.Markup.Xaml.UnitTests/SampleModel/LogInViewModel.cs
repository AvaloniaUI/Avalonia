// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using ReactiveUI;

namespace Perspex.Xaml.Base.UnitTest.SampleModel
{
    public class LogInViewModel : ReactiveObject
    {
        private string _username;

        public LogInViewModel()
        {
            this.OkCommand = ReactiveCommand.Create(
                this.WhenAnyValue(
                    x => x.Username,
                    x => !string.IsNullOrWhiteSpace(x)));
        }

        public string Username
        {
            get { return _username; }
            set { this.RaiseAndSetIfChanged(ref _username, value); }
        }

        public ReactiveCommand<object> OkCommand
        {
            get;
            private set;
        }
    }
}