using System;
using ReactiveUI;
using Avalonia.Animation;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Collections;
using System.Reactive.Linq;
using Avalonia.Markup.Xaml.Templates;

namespace RenderDemo.ViewModels
{

	public class XDataGridHeaderDescriptors
	{
		public int Index { get; set; }
		public string HeaderText { get; set; }
		public string PropertyName { get; set; }
		public string SharedSizeGroup => PropertyName + Index;
		internal int ColumnDefinitionIndex { get; set; }
	}

	public class SampleData
	{
		[DisplayName("Index")]
		public int Index { get; set; }

		[DisplayName("Some Text")]
		public string SomeText { get; set; }


		[DisplayName("Random Number 1")]
		public string RndNum1 { get; set; }

		[DisplayName("Random Number 2")]
		public string RndNum2 { get; set; }

		[DisplayName("Random Number 3")]
		public string RndNum3 { get; set; }
	}

	public class AnimationsPageViewModel : ReactiveObject
	{
		private static Random random = new Random();
		public static string RandomString(int length)
		{
			const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
			return new string(Enumerable.Repeat(chars, length)
			  .Select(s => s[random.Next(s.Length)]).ToArray());
		}

		public AnimationsPageViewModel()
		{

			this.WhenAnyValue(x => x.DataType)
				.Where(x => x != null)
				.Subscribe(AutoGenerateHeaders);

			var k = new AvaloniaList<object>();
			var r = new Random();

			for (int i = 0; i < 100_000; i++)
			{
				var l = new SampleData()
				{
					Index = i,
					SomeText = RandomString(10),
					RndNum1 = r.NextDouble().ToString(),
					RndNum2 = r.NextDouble().ToString(),
					RndNum3 = r.NextDouble().ToString(),
				};

				k.Add(l);
			}

			DataType = typeof(SampleData);
			DataRows = k;
		}

		private void AutoGenerateHeaders(Type obj)
		{
			int i = 0;
			var xdghList = new List<XDataGridHeaderDescriptors>();

			foreach (var property in obj.GetProperties())
			{
				var q = property.GetCustomAttributes(typeof(DisplayNameAttribute), false);

				if (!q.Any())
					continue;

				var attrib = (DisplayNameAttribute)q.Single();
				var dName = attrib.DisplayName;

				var xdgh = new XDataGridHeaderDescriptors()
				{
					Index = i,
					HeaderText = dName,
					PropertyName = property.Name,
				};

				i++;

				xdghList.Add(xdgh);
			}

			DataHeaderDescriptors = xdghList.ToArray();
		}

		Type dataType;
		public Type DataType
		{
			get => dataType;
			set => this.RaiseAndSetIfChanged(ref dataType, value);
		}


		


		XDataGridHeaderDescriptors[] dataHeaderDescriptors;
		public XDataGridHeaderDescriptors[] DataHeaderDescriptors
		{
			get => dataHeaderDescriptors;
			set => this.RaiseAndSetIfChanged(ref dataHeaderDescriptors, value);
		}

		IAvaloniaList<object> dataRows;

		public IAvaloniaList<object> DataRows
		{
			get => dataRows;
			set => this.RaiseAndSetIfChanged(ref dataRows, value);
		}
	}
}
