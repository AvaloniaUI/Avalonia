namespace ControlCatalog
{
    using Avalonia.Controls;
    using Avalonia.Media.Imaging;    
    using ReactiveUI;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Threading.Tasks;

    public abstract class DocumentTabViewModel : DocumentTabViewModel<object>
    {
        public DocumentTabViewModel() : base(null)
        {
        }
    }

    public abstract class DocumentTabViewModel<T> : ReactiveObject, IDocumentTabViewModel where T : class
    {
        private Dock dock;
        private string title;
        private bool _isTemporary;
        private bool _isHidden;
        private bool _isSelected;

        public DocumentTabViewModel(T model)
        {
            Dock = Dock.Left;

            IsVisible = true;
        }

        public Dock Dock
        {
            get { return dock; }
            set { this.RaiseAndSetIfChanged(ref dock, value); }
        }

        public virtual void Save()
        {

        }

        public string Title
        {
            get => title;
            set
            {
                this.RaiseAndSetIfChanged(ref title, value);
            }
        }

        public bool IsTemporary
        {
            get
            {
                return _isTemporary;
            }
            set
            {
                if (value)
                {
                    Dock = Dock.Right;
                }
                else
                {
                    Dock = Dock.Left;
                }

                this.RaiseAndSetIfChanged(ref _isTemporary, value);
            }
        }

        public bool IsVisible
        {
            get { return _isHidden; }
            set { this.RaiseAndSetIfChanged(ref _isHidden, value); }
        }

        public bool IsSelected
        {
            get { return _isSelected; }
            set { this.RaiseAndSetIfChanged(ref _isSelected, value); }
        }

        public virtual void Close()
        {
            DocumentTabControlViewModel.Instance.CloseDocument(this);
        }

        public virtual void Open()
        {

        }
    }

    public class WelcomeScreenViewModel : DocumentTabViewModel
    {
        private ObservableCollection<RecentProjectViewModel> _recentProjects;        

        public WelcomeScreenViewModel()
        {
            Title = "Start Page";

            _recentProjects = new ObservableCollection<RecentProjectViewModel>();            

            LoadRecentProjects();
        }

        ~WelcomeScreenViewModel()
        {

        }

        public override void Open()
        {
            base.Open();                       
        }

        public override void Close()
        {
            base.Close();
        }        

        public void Activation()
        {   
        }

        public void BeforeActivation()
        {
        }

        private void LoadRecentProjects()
        {
            _recentProjects.Clear();

            for (int i = 0; i < 8; i++)
            {
                _recentProjects.Add(new RecentProjectViewModel($"RecentProject {i}", "c:\\mypath"));
            }
        }

        public ObservableCollection<RecentProjectViewModel> RecentProjects
        {
            get { return _recentProjects; }
            set { this.RaiseAndSetIfChanged(ref _recentProjects, value); }
        }

        public ReactiveCommand NewSolution { get; }
        public ReactiveCommand OpenSolution { get; }
    }
}