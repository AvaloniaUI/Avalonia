namespace DataGridAsyncDemoMVVM
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using Avalonia.Controls;
    using Avalonia.Threading;
    using CommunityToolkit.Mvvm.ComponentModel;
    using ReactiveUI;
    using ViewModels;
    using VitalElement.DataVirtualization;
    using VitalElement.DataVirtualization.DataManagement;
    using VitalElement.DataVirtualization.Pageing;


    public abstract partial class DataSourceSingleSelectViewModel1<TViewModel, TModel> : ViewModelBase
        where TViewModel : class
    {
        [ObservableProperty]
        private TViewModel? selectedItem;

        [ObservableProperty]
        private IDataItem<TViewModel>? listSelectedItem;

        private readonly DataSource<TViewModel, TModel> _dataSource;
        private readonly Action<TViewModel> _setter;
        private readonly bool _preLoadFirstPage;

        /// <inheritdoc/>
        protected DataSourceSingleSelectViewModel1(DataSource<TViewModel, TModel> dataSource,
            Action<TViewModel?> setter, bool preLoadFirstPage = true)
        {
            _dataSource = dataSource;
            _setter = setter;
            _preLoadFirstPage = preLoadFirstPage;
        }

        public IReadOnlyCollection<IDataItem<TViewModel>> Options => DataSource.Collection;

        public DataSource<TViewModel, TModel> DataSource => _dataSource;

        public async Task SelectAsync(Expression<Func<TModel, bool>> predicate)
        {
            if (_preLoadFirstPage && !DataSource.IsInitialised)
            {
                await Task.CompletedTask;
            }

            SelectedItem = await DataSource.GetViewModelAsync(predicate);

            if (SelectedItem != null)
            {
                ListSelectedItem = DataItem.Create(SelectedItem);
            }
        }

        partial void OnListSelectedItemChanged(IDataItem<TViewModel>? value)
        {
            if (value != null)
            {
                SelectedItem = value.Item;
            }
        }

        partial void OnSelectedItemChanged(TViewModel? value)
        {
            _setter(value);
        }
    }

    public class RemoteItemSelector : DataSourceSingleSelectViewModel1<RemoteOrDbDataItem, RemoteOrDbDataItem>
    {
        public RemoteItemSelector(DataSource<RemoteOrDbDataItem, RemoteOrDbDataItem> dataSource, Action<RemoteOrDbDataItem> setter) : base(dataSource, setter, false)
        {
        }
    }

    public partial class MainViewModel : ViewModelBase
    {
        [ObservableProperty]
        private RemoteOrDbDataItem? _selectedItem;

        [ObservableProperty]
        private int _randomIndex;

        public MainViewModel()
        {
            var dataSource = new RemoteOrDbDataSource();

            Items = dataSource.Collection;

            Selector = new(dataSource, x => SelectedItem = x);

            SelectRandomCommand = ReactiveCommand.Create(() =>
            {//
                var rand = new Random((int)DateTime.Now.Ticks);

                var index = rand.Next(0, dataSource.Emulation.Items.Count);

                RandomIndex = index;

                //Selector.SelectedItem = DataItem.Create(dataSource.Emulation.Items[RandomIndex]);
            });
            
            Dispatcher.UIThread.Post(async () =>
            {
               // SelectedItem = DataItem.Create(dataSource.Emulation.Items[500]); 
            });
        }
        
        public RemoteItemSelector Selector { get;}

        public IReadOnlyCollection<IDataItem<RemoteOrDbDataItem>> Items { get; }
        
        public ICommand SelectRandomCommand { get; }
    }
}
