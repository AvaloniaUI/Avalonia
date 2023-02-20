using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;

namespace ControlCatalog.Pages
{
    public class SharePage : UserControl
    {
        private IReadOnlyList<IStorageFile> _files;

        public SharePage()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            this.Get<Button>("ShareText").Click += async (o, e) =>
            {
                var text = this.Get<TextBox>("ShareBox").Text ?? string.Empty;

                var share = TopLevel.GetTopLevel(this)?.ShareProvider;

                if (share != null)
                {
                    await share.ShareAsync(text!.ToDataObject());
                }
            };

            this.Get<Button>("OpenFile").Click += async delegate
            {
                var storageProvider = TopLevel.GetTopLevel(this)?.StorageProvider;

                if (storageProvider != null)
                {
                    var result = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
                    {
                        Title = "Open file",
                        AllowMultiple = this.Get<CheckBox>("SelectMultiple").IsChecked ?? true,
                    });

                    _files = result;

                    string list = string.Empty;

                    foreach (var file in _files)
                    {
                        list += file.Name + "\n";
                    }

                    this.Get<TextBlock>("FileText").Text = list;
                }
            };

            this.Get<Button>("ShareFile").Click += async (o, e) =>
            {
                if(_files != null && _files.Count> 0)
                {
                    var share = TopLevel.GetTopLevel(this)?.ShareProvider;

                    if (share != null)
                    {
                        await share.ShareAsync(_files.ToDataObject());
                    }
                }
            };

            this.Get<Button>("ShareStream").Click += async (o, e) =>
            {
                if (_files != null && _files.Count > 0)
                {
                    var share = TopLevel.GetTopLevel(this)?.ShareProvider;

                    if (share != null)
                    {
                        var file = _files.First();

                        using var stream = await file.OpenReadAsync();

                        var dataObject = stream.ToDataObject("test");
                        await share.ShareAsync(dataObject);
                    }
                }
            };
        }
    }
}
