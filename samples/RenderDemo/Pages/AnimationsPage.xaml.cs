using System.Collections.Generic;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using ReactiveUI;
using RenderDemo.ViewModels;
using System;
using System.Linq;
using Avalonia.Controls.Presenters;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Controls.Templates;
using System.Linq.Expressions;

namespace RenderDemo.Pages
{

	public class AnimationsPage : UserControl
	{
		private ContentPresenter _headerSection;

		public AnimationsPage()
		{
			InitializeComponent();
			var vm = new AnimationsPageViewModel();
			this.DataContext = vm;
			this._headerSection = this.FindControl<ContentPresenter>("HeaderSection");

			vm.WhenAnyValue(x => x.DataHeaderDescriptors)
			  .Where(x => x != null)
			  .Do(GenerateHeaders)
			  .Do(ActivateItemTemplateFunc)
			  .Subscribe();

		}

		private void ActivateItemTemplateFunc(XDataGridHeaderDescriptors[] obj)
		{
			this.FindControl<ItemsRepeater>("repeater").ItemTemplate
				 = new FuncDataTemplate<object>((rowData, _) =>
				 {
					 return GenerateCellDataTemplate(rowData, obj);
				 });
		}

		private IControl GenerateCellDataTemplate(object rowData, XDataGridHeaderDescriptors[] obj)
		{
			var rowRoot = new Grid
			{
				DataContext = rowData
			};

			var rowCellList = new List<IControl>();

			for (int i = 0; i < obj.Length; i++)
			{
				var headerDesc = obj[i];
				var colDefHeaderCell = new ColumnDefinition(GridLength.Parse("*"));
				var headerColWidthBinding = HeaderColWidths[i];

				headerColWidthBinding.WhenAnyValue(x => x.Width)
									 .Do(x => colDefHeaderCell.Width = x)
									 .Subscribe();

				var colDefGridSplitter = new ColumnDefinition(GridLength.Parse("Auto"));

				rowRoot.ColumnDefinitions.AddRange(new[] { colDefHeaderCell, colDefGridSplitter });

				var rowCell = new Grid();
				var boundCellContent = new TextBlock();
				var newBind = new Binding(headerDesc.PropertyName);
				boundCellContent.Bind(TextBlock.TextProperty, newBind);

				rowCell.Children.Add(boundCellContent);

				Grid.SetColumn(rowCell, headerDesc.ColumnDefinitionIndex);

				rowCellList.Add(rowCell);
			}


			headerRoot.WhenAnyValue(x => x.Width)
			.Subscribe(x => rowRoot.Width = x);

			rowRoot.Children.AddRange(rowCellList);

			return rowRoot;
		}

		public class BoundColumnWidth : ReactiveObject
		{
			GridLength _width;
			public GridLength Width
			{
				get => _width;
				set => this.RaiseAndSetIfChanged(ref _width, value);
			}
		}

		public Dictionary<int, BoundColumnWidth> HeaderColWidths
		  = new Dictionary<int, BoundColumnWidth>();
		private DockPanel dock;
		private Grid headerRoot;

		private void GenerateHeaders(XDataGridHeaderDescriptors[] headerDesc)
		{
			var colCount = headerDesc.Length;

			this.headerRoot = new Grid();
			var k = 0;
			this.dock = this.FindControl<DockPanel>("dock");
			dock.WhenAnyValue(x => x.Bounds)
                .DistinctUntilChanged()
                .Take(20)
                .ObserveOn(RxApp.MainThreadScheduler)
				.Subscribe(x => headerRoot.Width = x.Width);

			headerRoot.HorizontalAlignment = HorizontalAlignment.Left;

			for (int i = 0; i < colCount; i++)
			{
				var curDesc = headerDesc[i];

				var colDefHeaderCell = new ColumnDefinition(GridLength.Parse("*"));
				HeaderColWidths.Add(curDesc.Index, new BoundColumnWidth());

				colDefHeaderCell.WhenAnyValue(x => x.Width)
								.Do(x => HeaderColWidths[curDesc.Index].Width = x)
								.Subscribe();

				var colDefGridSplitter = new ColumnDefinition(GridLength.Parse("Auto"));

				headerRoot.ColumnDefinitions.AddRange(new[] { colDefHeaderCell, colDefGridSplitter });

				// Can be replaced with a DataHeaderCell class then
				// adding the binding as DataContext.
				var headerCell = new Grid();
				headerCell.Children.Add(new TextBlock() { Text = curDesc.HeaderText });
				curDesc.ColumnDefinitionIndex = k;
				Grid.SetColumn(headerCell, k);
				Grid.SetColumnSpan(headerCell, 1);
				k++;

				var gridSplitter = new GridSplitter();
				Grid.SetColumn(gridSplitter, k);
				Grid.SetColumnSpan(gridSplitter, 1);
				k++;

				headerRoot.Children.AddRange(new IControl[] { headerCell, gridSplitter });
			}

			headerRoot.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Parse("*")));

			_headerSection.Content = headerRoot;
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
		}
	}
}
