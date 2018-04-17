using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Avalonia.Controls {

    /// <summary>
    /// Control allow execute navigation wihtin itself.
    /// </summary>
    public class Frame : Panel
    {

        private const int MaxHistorySize = 15;

        public static readonly DirectProperty<Frame , bool> IsStackProperty =
                AvaloniaProperty.RegisterDirect<Frame , bool> (
                    nameof ( IsStack ) ,
                    frame => frame.IsStack ,
                    ( frame , isStack ) => frame.IsStack = isStack
                );

        public static readonly DirectProperty<Frame , Type> CurrentViewProperty =
                AvaloniaProperty.RegisterDirect<Frame , Type> (
                    nameof ( CurrentView ) ,
                    frame => frame.CurrentView ,
                    ( frame , currentView ) => frame.CurrentView = currentView
                );

        public static readonly DirectProperty<Frame , HistoryItem> CurrentStateProperty =
                AvaloniaProperty.RegisterDirect<Frame , HistoryItem> (
                    nameof ( CurrentState ) ,
                    frame => frame.CurrentState ,
                    ( frame , currentState ) => frame.CurrentState = currentState
                );

        private bool _isStack = false;

        private Type _currentView;

        private HistoryItem _currentState;

        private List<HistoryItem> _history = new List<HistoryItem> ();

        private IViewResolver _viewResolver = new SimpleViewResolver ();

        private void AddNewItemToHistory ( HistoryItem historyItem , int insertPosition = -1 )
        {
            if ( _history.Count == MaxHistorySize ) _history.RemoveAt ( 0 );

            if ( IsStack ) {
                _history.Add ( historyItem );
            }
            else {
                if ( insertPosition > -1 && insertPosition < _history.Count - 1 ) _history = _history.GetRange ( 0 , insertPosition + 1 );

                _history.Add ( historyItem );
            }
        }

        private void ChangeContent ( HistoryItem historyItem , HistoryItem oldHistoryItem , NavigationMode mode )
        {
            if ( Children.Count > 0 ) {
                if ( Children.First () is Page currentPage ) {
                    currentPage.Frame = null;
                    currentPage.NavigateFrom (
                        new NavigationEventArgs {
                            Mode = mode ,
                            Type = oldHistoryItem?.Type ,
                            Parameters = oldHistoryItem?.Parameters
                        }
                    );
                }
                Children.Clear ();
            }

            var control = _viewResolver.Resolve ( historyItem.Type );
            var page = control as Page;
            if ( page != null ) {
                page.Frame = this;
                page.NavigateTo (
                    new NavigationEventArgs {
                        Parameters = historyItem.Parameters.ToArray () ,
                        Type = historyItem.Type ,
                        Mode = mode
                    }
                );
            }

            Children.Add ( page );
        }

        private void RaiseStateProperties ( HistoryItem newElement )
        {
            _currentState = newElement;
            _currentView = newElement.Type;
            RaisePropertyChanged ( CurrentStateProperty , CurrentState , newElement );
            RaisePropertyChanged ( CurrentViewProperty , CurrentView , newElement.Type );
        }

        /// <summary>
        /// Go back from history.
        /// </summary>
        public void GoBack ()
        {
            if ( !CanGoBack () ) return;

            var currentItemIndex = _history.IndexOf ( CurrentState );
            var newElement = _history.ElementAt ( currentItemIndex - 1 );

            //stack mode when we delete history record immediatly after going back.
            if ( IsStack ) _history = _history.GetRange ( 0 , _history.Count - ( _history.Count - currentItemIndex ) );

            ChangeContent ( newElement , CurrentState , NavigationMode.Back );

            RaiseStateProperties ( newElement );
        }

        /// <summary>
        /// Go forward for one step.
        /// </summary>
        public void GoForward ()
        {
            if ( !CanGoForward () ) return;

            var currentIndex = _history.IndexOf ( CurrentState );
            if ( currentIndex == _history.Count () - 1 ) return;

            var newElement = _history.ElementAt ( currentIndex + 1 );

            ChangeContent ( newElement , CurrentState , NavigationMode.Forward );

            RaiseStateProperties ( newElement );
        }

        /// <summary>
        /// Is it possible to go back to the page?
        /// </summary>
        /// <returns></returns>
        public bool CanGoBack () => _history.IndexOf ( CurrentState ) > 0;

        /// <summary>
        /// Is it possible to go to the front page?
        /// </summary>
        /// <returns></returns>
        public bool CanGoForward () => _history.IndexOf ( CurrentState ) < _history.Count - 1;

        /// <summary>
        /// Current state of history.
        /// </summary>
        public HistoryItem CurrentState
        {
            get
            {
                return _currentState;
            }
            set
            {
                var selectedIndex = _history.IndexOf ( _currentState );

                SetAndRaise ( CurrentStateProperty , ref _currentState , value );

                if ( value == null ) return;

                AddNewItemToHistory ( value , selectedIndex );

                ChangeContent ( value , null , NavigationMode.New );

                _currentView = value.Type;
                RaisePropertyChanged ( CurrentViewProperty , CurrentView , value.Type );
            }
        }

        /// <summary>
        /// Current view of history (if you need a simple shift page, without parameters).
        /// </summary>
        public Type CurrentView
        {
            get
            {
                return _currentView;
            }
            set
            {
                var selectedIndex = _history.IndexOf ( _currentState );

                SetAndRaise ( CurrentViewProperty , ref _currentView , value );

                if ( value == null ) return;

                var historyItem = new HistoryItem {
                    Type = value ,
                    Parameters = Enumerable.Empty<HistoryItem> ()
                };

                AddNewItemToHistory ( historyItem , selectedIndex );

                ChangeContent ( historyItem , null , NavigationMode.New );

                _currentState = historyItem;
                RaisePropertyChanged ( CurrentStateProperty , CurrentState , historyItem );
            }
        }

        /// <summary>
        /// This parameter influence on 
        /// </summary>
        public bool IsStack
        {
            get
            {
                return _isStack;
            }
            set
            {
                SetAndRaise ( IsStackProperty , ref _isStack , value );
            }
        }

        /// <summary>
        /// Go to a new page.
        /// </summary>
        /// <param name="type">The type of page that will be displayed in the frame.</param>
        /// <param name="parameters">Parameters wiil be passed to </param>
        public void Navigate ( Type type , params object[] parameters )
        {
            CurrentState = new HistoryItem {
                Type = type ,
                Parameters = parameters
            };
        }

        /// <summary>
        /// Set class that will be resolve view types.
        /// </summary>
        /// <param name="viewResolver"></param>
        public void SetViewResolver ( IViewResolver viewResolver )
        {
            _viewResolver = viewResolver;
        }

        /// <summary>
        /// Export history.
        /// </summary>
        public Task Export ( IHistoryExport historyExport )
        {
            if ( historyExport == null ) throw new ArgumentNullException ( nameof ( historyExport ) );

            return historyExport.Export (
                _history.Select (
                    a =>
                        new HistoryItem {
                            Type = a.Type ,
                            Parameters = a.Parameters.ToArray ()
                        }
                    ) ,
                    _history.IndexOf ( CurrentState )
                );
        }

        /// <summary>
        /// Import history.
        /// </summary>
        public async Task Import ( IHistoryImport historyImport )
        {
            if ( historyImport == null ) throw new ArgumentNullException ( nameof ( historyImport ) );

            _history.Clear ();

            var (historyItems, selected) = await historyImport.Import ();

            _history.AddRange (
                historyItems.Select (
                    a => new HistoryItem {
                        Type = a.Type ,
                        Parameters = a.Parameters.ToList ()
                    }
                )
            );

            if ( selected < 0 ) throw new ArgumentOutOfRangeException ( nameof ( selected ) );

            var selectedItem = _history.ElementAt ( selected );
            ChangeContent ( selectedItem , null , NavigationMode.New );
            RaiseStateProperties ( selectedItem );
        }

    }

}