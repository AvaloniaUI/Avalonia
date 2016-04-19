namespace XamlTestApplication.ViewModels
{
    using ReactiveUI;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class ShellViewModel : ReactiveObject
    {
        private ShellViewModel()
        {
            documents = new ObservableCollection<EditorViewModel>();

            AddDocumentCommand = ReactiveCommand.Create();
            AddDocumentCommand.Subscribe(_ =>
            {
                Documents.Add(new EditorViewModel());
            });
        }

        public static ShellViewModel Instance = new ShellViewModel();

        private ObservableCollection<EditorViewModel> documents;
        public ObservableCollection<EditorViewModel> Documents
        {
            get { return documents; }
            set { this.RaiseAndSetIfChanged(ref documents, value); }
        }

        private EditorViewModel selectedDocument;

        public EditorViewModel SelectedDocument
        {
            get { return selectedDocument; }
            set { this.RaiseAndSetIfChanged(ref selectedDocument, value); }
        }


        public ReactiveCommand<object> AddDocumentCommand { get; }
    }

    public class EditorViewModel : ReactiveObject
    {
        public EditorViewModel()
        {
            text = "This is some text.";

            CloseCommand = ReactiveCommand.Create();

            CloseCommand.Subscribe(_ =>
            {
                ShellViewModel.Instance.Documents.Remove(this);
            });
        }

        ~EditorViewModel()
        {

        }

        private string text;
        public string Text
        {
            get { return text; }
            set { this.RaiseAndSetIfChanged(ref text, value); }
        }

        public ReactiveCommand<object> CloseCommand { get; }
    }
}
