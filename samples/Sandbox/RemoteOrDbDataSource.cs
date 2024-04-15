namespace DataGridAsyncDemoMVVM;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using VitalElement.DataVirtualization.DataManagement;
using VitalElement.DataVirtualization.Extensions;

public class RemoteOrDbDataSource : DataSource<RemoteOrDbDataItem, RemoteOrDbDataItem>
{
    private readonly IQueryable<RemoteOrDbDataItem> _remoteDatas;

    private readonly Random _rand = new();
    
    public RemoteOrDbDataSourceEmulation Emulation { get; }
        
    public RemoteOrDbDataSource() : base (x=>x, 50, 4)
    {
        this.CreateSortDescription(x => x.Id, ListSortDirection.Descending);
        
        Emulation = new RemoteOrDbDataSourceEmulation(100000);

        _remoteDatas = Emulation.Items.AsQueryable();
    }

    protected override void OnMaterialized(RemoteOrDbDataItem item)
    {
        // do nothing.
    }

    protected override Task<bool> DoCreateAsync(RemoteOrDbDataItem item)
    {
        throw new NotImplementedException();
    }

    protected override Task<bool> DoUpdateAsync(RemoteOrDbDataItem viewModel)
    {
        throw new NotImplementedException();
    }

    protected override Task<bool> DoDeleteAsync(RemoteOrDbDataItem item)
    {
        throw new NotImplementedException();
    }

    protected override void OnReset(int count)
    {
        // Do nothing.
    }

    protected override Task<bool> ContainsAsync(RemoteOrDbDataItem item)
    {
        throw new NotImplementedException();
    }

    public override Task<RemoteOrDbDataItem?> GetItemAsync(Expression<Func<RemoteOrDbDataItem, bool>> predicate)
    {
        return Task.FromResult((RemoteOrDbDataItem?)null);
    }

    protected override async Task<int> GetCountAsync(Func<IQueryable<RemoteOrDbDataItem>, IQueryable<RemoteOrDbDataItem>> filterQuery)
    {
        //await Task.Delay(1000 + (int)Math.Round(_rand.NextDouble() * 30));

        return await _remoteDatas.GetRowCountAsync(filterQuery);
    }

    protected override async Task<IEnumerable<RemoteOrDbDataItem>> GetItemsAtAsync(int offset, int count, Func<IQueryable<RemoteOrDbDataItem>, IQueryable<RemoteOrDbDataItem>> filterSortQuery)
    {
        if (count > 5)
        {
            await Task.Delay(1500 + (int)Math.Round(_rand.NextDouble() * 100));
        }

        return await _remoteDatas.GetRowsAsync(offset, count, filterSortQuery);
    }

    protected override RemoteOrDbDataItem? GetPlaceHolder(int index, int page, int offset)
    {
        return new RemoteOrDbDataItem(-1, "", "loading...", "", index, offset);
    }

    protected override bool ModelsEqual(RemoteOrDbDataItem a, RemoteOrDbDataItem b)
    {
        return a.Id == b.Id;
    }

    protected override RemoteOrDbDataItem? GetModelForViewModel(RemoteOrDbDataItem viewModel)
    {
        return viewModel;
    }
}