// -----------------------------------------------------------------------
// <copyright file="UserRepositoriesViewModel.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace GitHubClient.ViewModels
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Octokit;
    using ReactiveUI;

    public class UserRepositoriesViewModel : ReactiveObject
    {
        private IReadOnlyList<Repository> repositories;

        public async Task Load(string username)
        {
            var github = new GitHubClient(new ProductHeaderValue("PerspexGitHubClient"));
            this.Repositories = await github.Repository.GetAllForUser(username);
        }

        public IReadOnlyList<Repository> Repositories
        {
            get { return this.repositories; }
            private set { this.RaiseAndSetIfChanged(ref this.repositories, value); }
        }
    }
}