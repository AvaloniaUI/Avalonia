namespace DataGridAsyncDemoMVVM
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using Avalonia.Controls;
    using Avalonia.ReactiveUI;
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
        private IDataItem<TViewModel>? selectedItem;

        [ObservableProperty]
        private IDataItem<TViewModel>? listSelectedItem;

        private readonly DataSource<TViewModel, TModel> _dataSource;
        private readonly Action<IDataItem<TViewModel>> _setter;
        private readonly bool _preLoadFirstPage;

        /// <inheritdoc/>
        protected DataSourceSingleSelectViewModel1(DataSource<TViewModel, TModel> dataSource,
            Action<IDataItem<TViewModel>?> setter, bool preLoadFirstPage = true)
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

            var item = await DataSource.GetViewModelAsync(predicate);
            if (item is { })
            {
                SelectedItem = DataItem.Create(item);
            }

            if (SelectedItem != null)
            {
                ListSelectedItem = SelectedItem;
            }
        }

        partial void OnListSelectedItemChanged(IDataItem<TViewModel>? value)
        {
            if (value != null)
            {
                SelectedItem = value;
            }
        }

        partial void OnSelectedItemChanged(IDataItem<TViewModel>? value)
        {
            _setter(value);
        }
    }

    public class RemoteItemSelector : DataSourceSingleSelectViewModel1<RemoteOrDbDataItem, RemoteOrDbDataItem>
    {
        public RemoteItemSelector(DataSource<RemoteOrDbDataItem, RemoteOrDbDataItem> dataSource, Action<IDataItem<RemoteOrDbDataItem>> setter) : base(dataSource, setter, false)
        {
        }
    }

    public partial class MainViewModel : ViewModelBase
    {
        [ObservableProperty]
        private IDataItem<RemoteOrDbDataItem>? _selectedItem;

        [ObservableProperty]
        private int _randomIndex;

        public MainViewModel()
        {
            RxApp.MainThreadScheduler = AvaloniaScheduler.Instance;
            var dataSource = new RemoteOrDbDataSource();

            Selector = new(dataSource, x => SelectedItem = x);

            SelectRandomCommand = ReactiveCommand.Create(() =>
            {//
                var rand = new Random((int)DateTime.Now.Ticks);

                var index = rand.Next(0, dataSource.Emulation.Items.Count);

                RandomIndex = index;

                Selector.ListSelectedItem = DataItem.Create(dataSource.Emulation.Items[RandomIndex]);
            });
            
            Dispatcher.UIThread.Post(async () =>
            {
                Selector.ListSelectedItem = DataItem.Create(dataSource.Emulation.Items[500]); 
            });
        }
        
        public RemoteItemSelector Selector { get;}
        
        public ICommand SelectRandomCommand { get; }
    }
}
