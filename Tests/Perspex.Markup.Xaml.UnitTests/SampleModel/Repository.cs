namespace GitHubClient.ViewModels
{
    public class Repository
    {
        private readonly string name;

        public Repository(string name)
        {
            this.name = name;
        }

        public string Name
        {
            get { return name; }
        }
    }
}