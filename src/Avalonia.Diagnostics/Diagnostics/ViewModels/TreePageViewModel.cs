using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace Avalonia.Diagnostics.ViewModels
{
    internal class TreePageViewModel : ViewModelBase, IDisposable, INotifyDataErrorInfo
    {
        private readonly Dictionary<string, string> _errors = new Dictionary<string, string>();
        private TreeNode _selectedNode;
        private ControlDetailsViewModel _details;
        private string _propertyFilter = string.Empty;
        private bool _useRegexFilter;
        private IEnumerable<Models.FavoriteProperties> _favoritesProperties = null;
        private Models.FavoriteProperties _favoriteProperties;

        public TreePageViewModel(MainViewModel mainView, TreeNode[] nodes)
        {
            MainView = mainView;
            Nodes = nodes;
        }

        public MainViewModel MainView { get; }

        public TreeNode[] Nodes { get; protected set; }

        public TreeNode SelectedNode
        {
            get => _selectedNode;
            private set
            {
                if (RaiseAndSetIfChanged(ref _selectedNode, value))
                {
                    Details = value != null ?
                        new ControlDetailsViewModel(this, value.Visual) :
                        null;
                }
            }
        }

        public ControlDetailsViewModel Details
        {
            get => _details;
            private set
            {
                var oldValue = _details;

                if (RaiseAndSetIfChanged(ref _details, value))
                {
                    oldValue?.Dispose();
                }
            }
        }

        public Regex FilterRegex { get; set; }

        private void UpdateFilterRegex()
        {
            void ClearError()
            {
                if (_errors.Remove(nameof(PropertyFilter)))
                {
                    ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(PropertyFilter)));
                }
            }

            if (UseRegexFilter)
            {
                try
                {
                    FilterRegex = new Regex(PropertyFilter, RegexOptions.Compiled);
                    ClearError();
                }
                catch (Exception exception)
                {
                    _errors[nameof(PropertyFilter)] = exception.Message;
                    ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(PropertyFilter)));
                }
            }
            else
            {
                ClearError();
            }
        }

        public string PropertyFilter
        {
            get => _propertyFilter;
            set
            {
                if (RaiseAndSetIfChanged(ref _propertyFilter, value))
                {
                    UpdateFilterRegex();
                    Details.PropertiesView.Refresh();
                }
            }
        }

        public bool UseRegexFilter
        {
            get => _useRegexFilter;
            set
            {
                if (RaiseAndSetIfChanged(ref _useRegexFilter, value))
                {
                    UpdateFilterRegex();
                    Details.PropertiesView.Refresh();
                }
            }
        }

        public void Dispose()
        {
            foreach (var node in Nodes)
            {
                node.Dispose();
            }

            _details?.Dispose();
        }

        public TreeNode FindNode(IControl control)
        {
            foreach (var node in Nodes)
            {
                var result = FindNode(node, control);

                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        public void SelectControl(IControl control)
        {
            var node = default(TreeNode);

            while (node == null && control != null)
            {
                node = FindNode(control);

                if (node == null)
                {
                    control = control.GetVisualParent<IControl>();
                }
            }

            if (node != null)
            {
                SelectedNode = node;
                ExpandNode(node.Parent);
            }
        }

        private void ExpandNode(TreeNode node)
        {
            if (node != null)
            {
                node.IsExpanded = true;
                ExpandNode(node.Parent);
            }
        }

        private TreeNode FindNode(TreeNode node, IControl control)
        {
            if (node.Visual == control)
            {
                return node;
            }
            else
            {
                foreach (var child in node.Children)
                {
                    var result = FindNode(child, control);

                    if (result != null)
                    {
                        return result;
                    }
                }
            }

            return null;
        }

        public IEnumerable GetErrors(string propertyName)
        {
            if (_errors.TryGetValue(propertyName, out var error))
            {
                yield return error;
            }
        }

        public bool HasErrors => _errors.Count > 0;

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        public IEnumerable<Models.FavoriteProperties> FavoritesProperties
        {
            get
            {
                if (_favoritesProperties == null)
                {
                    var favoritePath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)
                        , nameof(FavoritesProperties) + ".json");
                    if (System.IO.File.Exists(favoritePath))
                    {
                        try
                        {
                            using (var file = System.IO.File.OpenText(favoritePath))
                            {
                                var serializer = new Newtonsoft.Json.JsonSerializer();
                                _favoritesProperties = (IEnumerable<Models.FavoriteProperties>)serializer.Deserialize(file, typeof(Models.FavoriteProperties[]));
                            }

                        }
                        catch (Exception)
                        {
                            _favoritesProperties = Models.FavoriteProperties.Default;
                        }

                    }
                    else
                    {
                        _favoritesProperties = Models.FavoriteProperties.Default;
                    }
                }
                return _favoritesProperties;
            }
            private set
            {
                RaiseAndSetIfChanged(ref _favoritesProperties, value);
            }
        }

        public Models.FavoriteProperties FavoriteProperties
        {
            get => _favoriteProperties;
            set
            {
                if (RaiseAndSetIfChanged(ref _favoriteProperties, value))
                {
                    UpdateFilterRegex();
                    Details.PropertiesView.Refresh();
                }
            }
        }

        void EditFavorites(object parameter)
        {
            Window owner = parameter as Window;
            var dialog = new Views.EditFavoritesPropertiesDialog(FavoritesProperties);

            dialog.ShowDialog<Models.FavoriteProperties[]>(owner)
                .ContinueWith(task =>
                {
                    var result = task.Result;
                    if (result != null)
                    {
                        FavoritesProperties = result;

                        var favoritePath = System.IO.Path
                            .Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)
                            , nameof(FavoritesProperties) + ".json");
                        try
                        {
                            using (var file = System.IO.File.CreateText(favoritePath))
                            {
                                var serializer = new Newtonsoft.Json.JsonSerializer();
                                serializer.Serialize(file, FavoritesProperties);
                            }

                        }
                        catch (Exception)
                        {
                            _favoritesProperties = Models.FavoriteProperties.Default;
                        }

                    }
                });
        }

    }
}
