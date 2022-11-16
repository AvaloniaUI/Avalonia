#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Dialogs.Internal;
using Avalonia.Platform.Storage;
using Avalonia.Platform.Storage.FileIO;

namespace Avalonia.Dialogs;

public class ManagedStorageProvider<T> : BclStorageProvider where T : Window, new()
{
    private readonly Window _parent;
    private readonly ManagedFileDialogOptions _managedOptions;

    public ManagedStorageProvider(Window parent, ManagedFileDialogOptions? managedOptions)
    {
        _parent = parent;
        _managedOptions = managedOptions ?? new ManagedFileDialogOptions();
    }

    public override bool CanSave => true;
    public override bool CanOpen => true;
    public override bool CanPickFolder => true;
            
    public override async Task<IReadOnlyList<IStorageFile>> OpenFilePickerAsync(FilePickerOpenOptions options)
    {
        var model = new ManagedFileChooserViewModel(options, _managedOptions);
        var results = await Show(model, _parent);

        return results.Select(f => new BclStorageFile(new FileInfo(f))).ToArray();
    }

    public override async Task<IStorageFile?> SaveFilePickerAsync(FilePickerSaveOptions options)
    {
        var model = new ManagedFileChooserViewModel(options, _managedOptions);
        var results = await Show(model, _parent);

        return results.FirstOrDefault() is { } result
            ? new BclStorageFile(new FileInfo(result))
            : null;
    }

    public override async Task<IReadOnlyList<IStorageFolder>> OpenFolderPickerAsync(FolderPickerOpenOptions options)
    {
        var model = new ManagedFileChooserViewModel(options, _managedOptions);
        var results = await Show(model, _parent);

        return results.Select(f => new BclStorageFolder(new DirectoryInfo(f))).ToArray();
    }
            
    private async Task<string[]> Show(ManagedFileChooserViewModel model, Window parent)
    {
        var dialog = new T
        {
            Content = new ManagedFileChooser(),
            Title = model.Title,
            DataContext = model
        };

        dialog.Closed += delegate { model.Cancel(); };

        string[]? result = null;
                
        model.CompleteRequested += items =>
        {
            result = items;
            dialog.Close();
        };

        model.OverwritePrompt += async (filename) =>
        {
            var overwritePromptDialog = new Window()
            {
                Title = "Confirm Save As",
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Padding = new Thickness(10),
                MinWidth = 270
            };

            string name = Path.GetFileName(filename);

            var panel = new DockPanel()
            {
                HorizontalAlignment = Layout.HorizontalAlignment.Stretch
            };

            var label = new Label()
            {
                Content = $"{name} already exists.\nDo you want to replace it?"
            };

            panel.Children.Add(label);
            DockPanel.SetDock(label, Dock.Top);

            var buttonPanel = new StackPanel()
            {
                HorizontalAlignment = Layout.HorizontalAlignment.Right,
                Orientation = Layout.Orientation.Horizontal,
                Spacing = 10
            };

            var button = new Button()
            {
                Content = "Yes",
                HorizontalAlignment = Layout.HorizontalAlignment.Right
            };

            button.Click += (sender, args) =>
            {
                result = new string[1] { filename };
                overwritePromptDialog.Close();
                dialog.Close();
            };

            buttonPanel.Children.Add(button);

            button = new Button()
            {
                Content = "No",
                HorizontalAlignment = Layout.HorizontalAlignment.Right
            };

            button.Click += (sender, args) =>
            {
                overwritePromptDialog.Close();
            };

            buttonPanel.Children.Add(button);

            panel.Children.Add(buttonPanel);
            DockPanel.SetDock(buttonPanel, Dock.Bottom);

            overwritePromptDialog.Content = panel;

            await overwritePromptDialog.ShowDialog(dialog);
        };

        model.CancelRequested += dialog.Close;

        await dialog.ShowDialog<object>(parent);
        return result ?? Array.Empty<string>();
    }
}
