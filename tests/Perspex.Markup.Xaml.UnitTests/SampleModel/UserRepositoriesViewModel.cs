// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using GitHubClient.ViewModels;
using ReactiveUI;

namespace Perspex.Xaml.Base.UnitTest.SampleModel
{
    public class UserRepositoriesViewModel : ReactiveObject
    {
        private IReadOnlyList<Repository> _repositories;

        public async Task Load(string username)
        {
            Repositories = await new Task<IReadOnlyList<Repository>>(() => new List<Repository> { new Repository("Blah"), new Repository("Bleh") });
        }

        public IReadOnlyList<Repository> Repositories
        {
            get { return _repositories; }
            private set { this.RaiseAndSetIfChanged(ref _repositories, value); }
        }
    }
}