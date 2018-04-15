using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Avalonia.Controls {

    /// <summary>
    /// Control allow execute navigation wihtin itself.
    /// </summary>
    public class Frame : Panel {

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

        private Type _CurrentView;

        private HistoryItem _CurrentState;

        private List<HistoryItem> _History = new List<HistoryItem> ();

        private IViewResolver _ViewResolver = new SimpleViewResolver ();

        private void AddNewItemToHistory ( HistoryItem historyItem , int insertPosition = -1 ) {
            if ( _History.Count == MaxHistorySize ) _History.RemoveAt ( 0 );

            if ( IsStack ) {
                _History.Add ( historyItem );
            }
            else {
                if ( insertPosition > -1 && insertPosition < _History.Count - 1 ) _History = _History.GetRange ( 0 , insertPosition + 1 );

                _History.Add ( historyItem );
            }
        }

        private void ChangeContent ( HistoryItem historyItem , HistoryItem oldHistoryItem , NavigationMode mode ) {
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

            var control = _ViewResolver.Resolve ( historyItem.Type );
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

        private void RaiseStateProperties ( HistoryItem newElement ) {
            _CurrentState = newElement;
            _CurrentView = newElement.Type;
            RaisePropertyChanged ( CurrentStateProperty , CurrentState , newElement );
            RaisePropertyChanged ( CurrentViewProperty , CurrentView , newElement.Type );
        }

        /// <summary>
        /// Go back from history.
        /// </summary>
        public void GoBack () {
            if ( !CanGoBack () ) return;

            var currentItemIndex = _History.IndexOf ( CurrentState );
            var newElement = _History.ElementAt ( currentItemIndex - 1 );

            //stack mode when we delete history record immediatly after going back.
            if ( IsStack ) _History = _History.GetRange ( 0 , _History.Count - ( _History.Count - currentItemIndex ) );

            ChangeContent ( newElement , CurrentState , NavigationMode.Back );

            RaiseStateProperties ( newElement );
        }

        /// <summary>
        /// Go forward for one step.
        /// </summary>
        public void GoForward () {
            if ( !CanGoForward () ) return;

            var currentIndex = _History.IndexOf ( CurrentState );
            if ( currentIndex == _History.Count () - 1 ) return;

            var newElement = _History.ElementAt ( currentIndex + 1 );

            ChangeContent ( newElement , CurrentState , NavigationMode.Forward );

            RaiseStateProperties ( newElement );
        }

        /// <summary>
        /// Is it possible to go back to the page?
        /// </summary>
        /// <returns></returns>
        public bool CanGoBack () => _History.IndexOf ( CurrentState ) > 0;

        /// <summary>
        /// Is it possible to go to the front page?
        /// </summary>
        /// <returns></returns>
        public bool CanGoForward () => _History.IndexOf ( CurrentState ) < _History.Count - 1;

        /// <summary>
        /// Current state of history.
        /// </summary>
        public HistoryItem CurrentState
        {
            get
            {
                return _CurrentState;
            }
            set
            {
                var selectedIndex = _History.IndexOf ( _CurrentState );

                SetAndRaise ( CurrentStateProperty , ref _CurrentState , value );

                if ( value == null ) return;

                AddNewItemToHistory ( value , selectedIndex );

                ChangeContent ( value , null , NavigationMode.New );

                _CurrentView = value.Type;
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
                return _CurrentView;
            }
            set
            {
                var selectedIndex = _History.IndexOf ( _CurrentState );

                SetAndRaise ( CurrentViewProperty , ref _CurrentView , value );

                if ( value == null ) return;

                var historyItem = new HistoryItem {
                    Type = value ,
                    Parameters = Enumerable.Empty<HistoryItem> ()
                };

                AddNewItemToHistory ( historyItem , selectedIndex );

                ChangeContent ( historyItem , null , NavigationMode.New );

                _CurrentState = historyItem;
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
        public void Navigate ( Type type , params object[] parameters ) {
            CurrentState = new HistoryItem {
                Type = type ,
                Parameters = parameters
            };
        }

        /// <summary>
        /// Set class that will be resolve view types.
        /// </summary>
        /// <param name="viewResolver"></param>
        public void SetViewResolver ( IViewResolver viewResolver ) {
            _ViewResolver = viewResolver;
        }

        /// <summary>
        /// Export history.
        /// </summary>
        public Task Export ( IHistoryExport historyExport ) {
            if ( historyExport == null ) throw new ArgumentNullException ( nameof ( historyExport ) );

            return historyExport.Export (
                _History.Select (
                    a =>
                        new HistoryItem {
                            Type = a.Type ,
                            Parameters = a.Parameters.ToArray ()
                        }
                    ) ,
                    _History.IndexOf ( CurrentState )
                );
        }

        /// <summary>
        /// Import history.
        /// </summary>
        public async Task Import ( IHistoryImport historyImport ) {
            if ( historyImport == null ) throw new ArgumentNullException ( nameof ( historyImport ) );

            _History.Clear ();

            var (historyItems, selected) = await historyImport.Import ();

            _History.AddRange (
                historyItems.Select (
                    a => new HistoryItem {
                        Type = a.Type ,
                        Parameters = a.Parameters.ToList ()
                    }
                )
            );

            if ( selected < 0 ) throw new ArgumentOutOfRangeException ( nameof ( selected ) );

            var selectedItem = _History.ElementAt ( selected );
            ChangeContent ( selectedItem , null , NavigationMode.New );
            RaiseStateProperties ( selectedItem );
        }

    }

}