using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Dialogs.Internal;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using Avalonia.Platform.Storage.FileIO;
using Avalonia.VisualTree;

namespace Avalonia.Dialogs;

internal class ManagedStorageProvider : BclStorageProvider
{
    private readonly TopLevel? _parent;
    private readonly ManagedFileDialogOptions _managedOptions;

    public ManagedStorageProvider(TopLevel? parent, ManagedFileDialogOptions? managedOptions = null)
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
        var results = await Show(model);

        return results.Select(f => new BclStorageFile(new FileInfo(f))).ToArray();
    }

    public override async Task<IStorageFile?> SaveFilePickerAsync(FilePickerSaveOptions options)
    {
        var model = new ManagedFileChooserViewModel(options, _managedOptions);
        var results = await Show(model);

        return results.FirstOrDefault() is { } result
            ? new BclStorageFile(new FileInfo(result))
            : null;
    }

    public override async Task<IReadOnlyList<IStorageFolder>> OpenFolderPickerAsync(FolderPickerOpenOptions options)
    {
        var model = new ManagedFileChooserViewModel(options, _managedOptions);
        var results = await Show(model);

        return results.Select(f => new BclStorageFolder(new DirectoryInfo(f))).ToArray();
    }

    private ContentControl PrepareRoot(ManagedFileChooserViewModel model)
    {
        var root = _managedOptions.ContentRootFactory?.Invoke();

        if (root is null)
        {
            if (_parent is not null and not Window)
            {
                root = new ContentControl();
            }
            else
            {
                root = new Window();
            }
        }

        root.Content = new ManagedFileChooser();
        root.DataContext = model;

        return root;
    }
    
    private Task<string[]> Show(ManagedFileChooserViewModel model)
    {
        var root = PrepareRoot(model);

        if (root is Window window)
        {
            return ShowAsWindow(window, model);
        }
        else if (_parent is not null)
        {
            return ShowAsPopup(root, model);
        }
        else
        {
            throw new InvalidOperationException(
                "Managed File Chooser requires existing parent or compatible windowing system.");
        }
    }

    private async Task<string[]> ShowAsWindow(Window window, ManagedFileChooserViewModel model)
    {
        var tcs = new TaskCompletionSource<bool>();
        window.Title = model.Title;
        window.Closed += delegate {
            model.Cancel();
            tcs.TrySetResult(true);
        };
        
        var result = Array.Empty<string>();
                
        model.CompleteRequested += items =>
        {
            result = items;
            window.Close();
        };

        model.OverwritePrompt += async (filename) =>
        {
            if (await ShowOverwritePrompt(filename, window))
            {
                window.Close();
            }
        };

        model.CancelRequested += window.Close;

        if (_parent is Window parent)
        {
            await window.ShowDialog<object>(parent);
        }
        else
        {
            window.Show();
        }

        await tcs.Task;

        return result;
    }
    
    private async Task<string[]> ShowAsPopup(ContentControl root, ManagedFileChooserViewModel model)
    {
        var tcs = new TaskCompletionSource<bool>();
        var rootPanel = _parent.FindDescendantOfType<Panel>()!;
        
        var popup = new Popup();
        popup.Placement = PlacementMode.Center;
        popup.IsLightDismissEnabled = false;
        popup.Child = root;
        popup.Width = _parent!.Width;
        popup.Height = _parent.Height;

        popup.Closed += delegate {
            model.Cancel();
            tcs.TrySetResult(true);
        };
        
        var result = Array.Empty<string>();
                
        model.CompleteRequested += items =>
        {
            result = items;
            popup.Close();
        };

        model.OverwritePrompt += async (filename) =>
        {
            if (await ShowOverwritePrompt(filename, root))
            {
                popup.Close();
            }
        };

        model.CancelRequested += delegate
        {
            popup.Close();
        };

        rootPanel.Children.Add(popup);
        _parent.SizeChanged += ParentOnSizeChanged;
        try
        {
            popup.Open();
            await tcs.Task;
        }
        finally
        {
            rootPanel.Children.Remove(popup);
            _parent.SizeChanged -= ParentOnSizeChanged;   
        }

        return result;
        
        void ParentOnSizeChanged(object? sender, SizeChangedEventArgs e)
        {
            if (!popup.IsOpen)
            {
                _parent.SizeChanged -= ParentOnSizeChanged;
            }
            
            popup.Width = _parent!.Width;
            popup.Height = _parent.Height;
        }
    }

    private static async Task<bool> ShowOverwritePrompt(string filename, ContentControl root)
    {
        var tcs = new TaskCompletionSource<bool>();
        var prompt = new ManagedFileChooserOverwritePrompt
        {
            FileName = Path.GetFileName(filename)
        };
        prompt.Result += (r) => tcs.TrySetResult(r);
            
        var flyout = new Flyout();
        flyout.Closed += (_, _) => tcs.TrySetResult(false);
        flyout.Content = prompt;
        flyout.Placement = PlacementMode.Center;
        flyout.ShowAt(root);

        var promptResult = await tcs.Task;
        flyout.Hide();

        return promptResult;
    }
}
