using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Media;
using MiniMvvm;

namespace ControlCatalog.Pages
{
    public sealed class NavigationPageMvvmShellViewModel : ViewModelBase
    {
        private readonly ISampleNavigationService _navigationService;
        private string _currentPageHeader = "Not initialized";
        private string _lastAction = "Waiting for first load";
        private int _navigationDepth;
        private ProjectCardViewModel? _selectedProject;

        internal NavigationPageMvvmShellViewModel(ISampleNavigationService navigationService)
        {
            _navigationService = navigationService;
            _navigationService.StateChanged += OnStateChanged;

            Workspace = new WorkspaceViewModel(CreateProjects(navigationService));
            SelectedProject = Workspace.Projects[0];

            OpenSelectedProjectCommand = MiniCommand.CreateFromTask(OpenSelectedProjectAsync);
            GoBackCommand = MiniCommand.CreateFromTask(_navigationService.GoBackAsync);
            PopToRootCommand = MiniCommand.CreateFromTask(_navigationService.PopToRootAsync);
        }

        internal WorkspaceViewModel Workspace { get; }

        public IReadOnlyList<ProjectCardViewModel> Projects => Workspace.Projects;

        public MiniCommand OpenSelectedProjectCommand { get; }

        public MiniCommand GoBackCommand { get; }

        public MiniCommand PopToRootCommand { get; }

        public string CurrentPageHeader
        {
            get => _currentPageHeader;
            set => this.RaiseAndSetIfChanged(ref _currentPageHeader, value);
        }

        public int NavigationDepth
        {
            get => _navigationDepth;
            set => this.RaiseAndSetIfChanged(ref _navigationDepth, value);
        }

        public string LastAction
        {
            get => _lastAction;
            set => this.RaiseAndSetIfChanged(ref _lastAction, value);
        }

        public ProjectCardViewModel? SelectedProject
        {
            get => _selectedProject;
            set => this.RaiseAndSetIfChanged(ref _selectedProject, value);
        }

        public Task InitializeAsync() => _navigationService.NavigateToAsync(Workspace);

        private async Task OpenSelectedProjectAsync()
        {
            if (SelectedProject == null)
                return;

            await SelectedProject.OpenCommandAsync();
        }

        private void OnStateChanged(object? sender, NavigationStateChangedEventArgs e)
        {
            CurrentPageHeader = e.CurrentPageHeader;
            NavigationDepth = e.NavigationDepth;
            LastAction = e.LastAction;
        }

        private static IReadOnlyList<ProjectCardViewModel> CreateProjects(ISampleNavigationService navigationService) =>
            new[]
            {
                new ProjectCardViewModel(
                    "Release Radar",
                    "Marta Collins",
                    "Ready for QA",
                    "Coordinate the 11.0 release checklist and lock down the final regression window.",
                    "Freeze build on Friday",
                    Color.Parse("#0063B1"),
                    navigationService,
                    new[]
                    {
                        "Release notes draft updated with accessibility fixes.",
                        "Package validation finished for desktop artifacts.",
                        "Remaining task, confirm browser smoke test coverage."
                    }),
                new ProjectCardViewModel(
                    "Support Console",
                    "Jae Kim",
                    "Active Sprint",
                    "Consolidate customer incidents into a triage board and route them to platform owners.",
                    "Triage review in 2 hours",
                    Color.Parse("#0F7B0F"),
                    navigationService,
                    new[]
                    {
                        "Five customer reports grouped under input routing.",
                        "Hotfix candidate approved for preview branch.",
                        "Awaiting macOS verification on native embed scenarios."
                    }),
                new ProjectCardViewModel(
                    "Docs Refresh",
                    "Anika Patel",
                    "Needs Review",
                    "Refresh navigation samples and walkthrough docs so the gallery matches the current API.",
                    "Sample review tomorrow",
                    Color.Parse("#8E562E"),
                    navigationService,
                    new[]
                    {
                        "NavigationPage sample matrix reviewed with design.",
                        "MVVM walkthrough draft linked from the docs backlog.",
                        "Outstanding task, capture one more screenshot for drawer navigation."
                    }),
            };
    }

    internal sealed class WorkspaceViewModel : ViewModelBase
    {
        public WorkspaceViewModel(IReadOnlyList<ProjectCardViewModel> projects)
        {
            Projects = projects;
        }

        public string Title => "Team Workspace";

        public string Description =>
            "Each card is a project view model with its own command. The command asks ISampleNavigationService to navigate with the next view model, and SamplePageFactory resolves the matching ContentPage.";

        public IReadOnlyList<ProjectCardViewModel> Projects { get; }
    }

    public sealed class ProjectCardViewModel : ViewModelBase
    {
        private readonly ISampleNavigationService _navigationService;

        internal ProjectCardViewModel(
            string name,
            string owner,
            string status,
            string summary,
            string nextMilestone,
            Color accentColor,
            ISampleNavigationService navigationService,
            IReadOnlyList<string> activityItems)
        {
            Name = name;
            Owner = owner;
            Status = status;
            Summary = summary;
            NextMilestone = nextMilestone;
            AccentColor = accentColor;
            ActivityItems = activityItems;
            _navigationService = navigationService;

            OpenCommand = MiniCommand.CreateFromTask(OpenCommandAsync);
        }

        public string Name { get; }

        public string Owner { get; }

        public string Status { get; }

        public string Summary { get; }

        public string NextMilestone { get; }

        public Color AccentColor { get; }

        public IReadOnlyList<string> ActivityItems { get; }

        public MiniCommand OpenCommand { get; }

        public Task OpenCommandAsync()
        {
            return _navigationService.NavigateToAsync(new ProjectDetailViewModel(this, _navigationService));
        }
    }

    internal sealed class ProjectDetailViewModel : ViewModelBase
    {
        private readonly ProjectCardViewModel _project;
        private readonly ISampleNavigationService _navigationService;

        public ProjectDetailViewModel(ProjectCardViewModel project, ISampleNavigationService navigationService)
        {
            _project = project;
            _navigationService = navigationService;
            OpenActivityCommand = MiniCommand.CreateFromTask(OpenActivityAsync);
        }

        public string Name => _project.Name;

        public string Owner => _project.Owner;

        public string Status => _project.Status;

        public string Summary => _project.Summary;

        public string NextMilestone => _project.NextMilestone;

        public Color AccentColor => _project.AccentColor;

        public MiniCommand OpenActivityCommand { get; }

        private Task OpenActivityAsync()
        {
            return _navigationService.NavigateToAsync(new ProjectActivityViewModel(_project));
        }
    }

    internal sealed class ProjectActivityViewModel : ViewModelBase
    {
        public ProjectActivityViewModel(ProjectCardViewModel project)
        {
            Name = project.Name;
            AccentColor = project.AccentColor;
            Items = project.ActivityItems;
        }

        public string Name { get; }

        public Color AccentColor { get; }

        public IReadOnlyList<string> Items { get; }
    }
}
