





namespace Perspex.Xaml.Base.UnitTest.SampleModel
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using GitHubClient.ViewModels;
    using ReactiveUI;

    public class UserRepositoriesViewModel : ReactiveObject
    {
        private IReadOnlyList<Repository> repositories;

        public async Task Load(string username)
        {            
            this.Repositories = await new Task<IReadOnlyList<Repository>>(() => new List<Repository> { new Repository("Blah"), new Repository("Bleh") });
        }

        public IReadOnlyList<Repository> Repositories
        {
            get { return this.repositories; }
            private set { this.RaiseAndSetIfChanged(ref this.repositories, value); }
        }
    }
}